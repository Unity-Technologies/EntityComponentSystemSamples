using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Physics.Math;

namespace Unity.Physics.Extensions
{
    // A mouse pick collector which stores every hit. Based off the ClosestHitCollector
    [BurstCompile]
    public struct MousePickCollector : ICollector<RaycastHit>
    {
        public bool IgnoreTriggers;
        public bool IgnoreStatic;
        public NativeArray<RigidBody> Bodies;
        public int NumDynamicBodies;

        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits { get; private set; }

        private RaycastHit m_ClosestHit;
        public RaycastHit Hit => m_ClosestHit;

        public MousePickCollector(float maxFraction, NativeArray<RigidBody> rigidBodies, int numDynamicBodies)
        {
            m_ClosestHit = default(RaycastHit);
            MaxFraction = maxFraction;
            NumHits = 0;
            IgnoreTriggers = true;
            IgnoreStatic = true;
            Bodies = rigidBodies;
            NumDynamicBodies = numDynamicBodies;
        }

        #region ICollector

        public bool AddHit(RaycastHit hit)
        {
            Assert.IsTrue(hit.Fraction <= MaxFraction);

            var isAcceptable = true;
            if (IgnoreStatic)
            {
                isAcceptable = isAcceptable && (hit.RigidBodyIndex >= 0) && (hit.RigidBodyIndex < NumDynamicBodies);
            }
            if (IgnoreTriggers)
            {
                isAcceptable = isAcceptable && hit.Material.CollisionResponse != CollisionResponsePolicy.RaiseTriggerEvents;
            }

            if (!isAcceptable)
            {
                return false;
            }

            MaxFraction = hit.Fraction;
            m_ClosestHit = hit;
            NumHits = 1;
            return true;
        }

        #endregion
    }

    public struct MousePick : IComponentData
    {
        public bool IgnoreTriggers;
        public bool IgnoreStatic;
    }

    [DisallowMultipleComponent]
    public class MousePickAuthoring : MonoBehaviour
    {
        public bool IgnoreTriggers = true;
        public bool IgnoreStatic = true;

        // Note: override OnEnable to be able to disable the component in the editor
        protected void OnEnable() {}
    }

    class MousePickBaker : Baker<MousePickAuthoring>
    {
        public override void Bake(MousePickAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MousePick()
            {
                IgnoreTriggers = authoring.IgnoreTriggers,
                IgnoreStatic = authoring.IgnoreStatic
            });
        }
    }

    // Attaches a virtual spring to the picked entity
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial class MousePickSystem : SystemBase
    {
        public const float k_MaxDistance = 100.0f;
        public NativeReference<SpringData> SpringDataRef;
        public JobHandle? PickJobHandle;

        public struct SpringData
        {
            public Entity Entity;
            public bool Dragging;
            public float3 PointOnBody;
            public float MouseDepth;
        }

        [BurstCompile]
        struct Pick : IJob
        {
            [ReadOnly] public CollisionWorld CollisionWorld;
            public NativeReference<SpringData> SpringDataRef;
            public RaycastInput RayInput;
            public float Near;
            public float3 Forward;
            [ReadOnly] public bool IgnoreTriggers;

            public void Execute()
            {
                var mousePickCollector = new MousePickCollector(1.0f, CollisionWorld.Bodies, CollisionWorld.NumDynamicBodies);
                mousePickCollector.IgnoreTriggers = IgnoreTriggers;

                if (CollisionWorld.CastRay(RayInput, ref mousePickCollector))
                {
                    float fraction = mousePickCollector.Hit.Fraction;
                    RigidBody hitBody = CollisionWorld.Bodies[mousePickCollector.Hit.RigidBodyIndex];

                    MTransform bodyFromWorld = Inverse(new MTransform(hitBody.WorldFromBody));
                    float3 pointOnBody = Mul(bodyFromWorld, mousePickCollector.Hit.Position);

                    SpringDataRef.Value = new SpringData
                    {
                        Entity = hitBody.Entity,
                        Dragging = true,
                        PointOnBody = pointOnBody,
                        MouseDepth = Near + math.dot(math.normalize(RayInput.End - RayInput.Start), Forward) * fraction * k_MaxDistance,
                    };
                }
                else
                {
                    SpringDataRef.Value = new SpringData
                    {
                        Dragging = false
                    };
                }
            }
        }

        public MousePickSystem()
        {
            SpringDataRef = new NativeReference<SpringData>(Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            SpringDataRef.Value = new SpringData();
        }

        protected override void OnCreate()
        {
            RequireForUpdate<MousePick>();
        }

        protected override void OnDestroy()
        {
            SpringDataRef.Dispose();
        }

        protected override void OnUpdate()
        {
            if (Input.GetMouseButtonDown(0) && (Camera.main != null))
            {
                Vector2 mousePosition = Input.mousePosition;
                UnityEngine.Ray unityRay = Camera.main.ScreenPointToRay(mousePosition);

                var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

                // Schedule picking job, after the collision world has been built
                Dependency = new Pick
                {
                    CollisionWorld = world.CollisionWorld,
                    SpringDataRef = SpringDataRef,
                    RayInput = new RaycastInput
                    {
                        Start = unityRay.origin,
                        End = unityRay.origin + unityRay.direction * k_MaxDistance,
                        Filter = CollisionFilter.Default,
                    },
                    Near = Camera.main.nearClipPlane,
                    Forward = Camera.main.transform.forward,
                    IgnoreTriggers = SystemAPI.GetSingleton<MousePick>().IgnoreTriggers,
                }.Schedule(Dependency);

                PickJobHandle = Dependency;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (PickJobHandle != null)
                {
                    PickJobHandle.Value.Complete();
                }
                SpringDataRef.Value = new SpringData();
            }
        }
    }

    // Applies any mouse spring as a change in velocity on the entity's motion component
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    public partial class MouseSpringSystem : SystemBase
    {
        MousePickSystem m_PickSystem;

        protected override void OnCreate()
        {
            m_PickSystem = World.GetOrCreateSystemManaged<MousePickSystem>();
            RequireForUpdate<MousePick>();
        }

        protected override void OnUpdate()
        {
            ComponentLookup<LocalTransform> LocalTransforms = GetComponentLookup<LocalTransform>(true);

            ComponentLookup<PhysicsVelocity> Velocities = GetComponentLookup<PhysicsVelocity>();
            ComponentLookup<PhysicsMass> Masses = GetComponentLookup<PhysicsMass>(true);
            ComponentLookup<PhysicsMassOverride> MassOverrides = GetComponentLookup<PhysicsMassOverride>(true);

            // If there's a pick job, wait for it to finish
            if (m_PickSystem.PickJobHandle != null)
            {
                JobHandle.CombineDependencies(Dependency, m_PickSystem.PickJobHandle.Value).Complete();
            }

            // If there's a picked entity, drag it
            MousePickSystem.SpringData springData = m_PickSystem.SpringDataRef.Value;
            if (springData.Dragging)
            {
                Entity entity = springData.Entity;
                if (!Masses.HasComponent(entity))
                {
                    return;
                }

                PhysicsMass massComponent = Masses[entity];
                PhysicsVelocity velocityComponent = Velocities[entity];

                // if body is kinematic
                // TODO: you should be able to rotate a body with infinite mass but finite inertia
                if (massComponent.HasInfiniteMass || MassOverrides.HasComponent(entity) && MassOverrides[entity].IsKinematic != 0)
                {
                    return;
                }


                var worldFromBody = new MTransform(LocalTransforms[entity].Rotation, LocalTransforms[entity].Position);


                // Body to motion transform
                var bodyFromMotion = new MTransform(Masses[entity].InertiaOrientation, Masses[entity].CenterOfMass);
                MTransform worldFromMotion = Mul(worldFromBody, bodyFromMotion);

                // TODO: shouldn't damp where inertia mass or inertia
                // Damp the current velocity
                const float gain = 0.95f;
                velocityComponent.Linear *= gain;
                velocityComponent.Angular *= gain;

                // Get the body and mouse points in world space
                float3 pointBodyWs = Mul(worldFromBody, springData.PointOnBody);
                float3 pointSpringWs = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, springData.MouseDepth));

                // Calculate the required change in velocity
                float3 pointBodyLs = Mul(Inverse(bodyFromMotion), springData.PointOnBody);
                float3 deltaVelocity;
                {
                    float3 pointDiff = pointBodyWs - pointSpringWs;
                    float3 relativeVelocityInWorld = velocityComponent.Linear + math.mul(worldFromMotion.Rotation, math.cross(velocityComponent.Angular, pointBodyLs));

                    const float elasticity = 0.1f;
                    const float damping = 0.5f;
                    deltaVelocity = -pointDiff * (elasticity / SystemAPI.Time.DeltaTime) - damping * relativeVelocityInWorld;
                }

                // Build effective mass matrix in world space
                // TODO how are bodies with inf inertia and finite mass represented
                // TODO the aggressive damping is hiding something wrong in this code if dragging non-uniform shapes
                float3x3 effectiveMassMatrix;
                {
                    float3 arm = pointBodyWs - worldFromMotion.Translation;
                    var skew = new float3x3(
                        new float3(0.0f, arm.z, -arm.y),
                        new float3(-arm.z, 0.0f, arm.x),
                        new float3(arm.y, -arm.x, 0.0f)
                    );

                    // world space inertia = worldFromMotion * inertiaInMotionSpace * motionFromWorld
                    var invInertiaWs = new float3x3(
                        massComponent.InverseInertia.x * worldFromMotion.Rotation.c0,
                        massComponent.InverseInertia.y * worldFromMotion.Rotation.c1,
                        massComponent.InverseInertia.z * worldFromMotion.Rotation.c2
                    );
                    invInertiaWs = math.mul(invInertiaWs, math.transpose(worldFromMotion.Rotation));

                    float3x3 invEffMassMatrix = math.mul(math.mul(skew, invInertiaWs), skew);
                    invEffMassMatrix.c0 = new float3(massComponent.InverseMass, 0.0f, 0.0f) - invEffMassMatrix.c0;
                    invEffMassMatrix.c1 = new float3(0.0f, massComponent.InverseMass, 0.0f) - invEffMassMatrix.c1;
                    invEffMassMatrix.c2 = new float3(0.0f, 0.0f, massComponent.InverseMass) - invEffMassMatrix.c2;

                    effectiveMassMatrix = math.inverse(invEffMassMatrix);
                }

                // Calculate impulse to cause the desired change in velocity
                float3 impulse = math.mul(effectiveMassMatrix, deltaVelocity);

                // Clip the impulse
                const float maxAcceleration = 250.0f;
                float maxImpulse = math.rcp(massComponent.InverseMass) * SystemAPI.Time.DeltaTime * maxAcceleration;
                impulse *= math.min(1.0f, math.sqrt((maxImpulse * maxImpulse) / math.lengthsq(impulse)));

                // Apply the impulse
                {
                    velocityComponent.Linear += impulse * massComponent.InverseMass;

                    float3 impulseLs = math.mul(math.transpose(worldFromMotion.Rotation), impulse);
                    float3 angularImpulseLs = math.cross(pointBodyLs, impulseLs);
                    velocityComponent.Angular += angularImpulseLs * massComponent.InverseInertia;
                }

                // Write back velocity
                Velocities[entity] = velocityComponent;
            }
        }
    }
}
