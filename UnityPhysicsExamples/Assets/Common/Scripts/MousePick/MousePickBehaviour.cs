using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Physics.Math;

namespace Unity.Physics.Extensions
{
    public struct MousePick : IComponentData { }

    public class MousePickBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new MousePick());
        }
    }

    // Attaches a virtual spring to the picked entity
    [UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class MousePickSystem : JobComponentSystem
    {
        const float k_MaxDistance = 100.0f;

        EntityQuery m_MouseGroup;
        BuildPhysicsWorld m_BuildPhysicsWorldSystem;

        public NativeArray<SpringData> SpringDatas;
        public JobHandle? PickJobHandle;

        public struct SpringData
        {
            public Entity Entity;
            public int Dragging; // bool isn't blittable
            public float3 PointOnBody;
            public float MouseDepth;
        }

        [BurstCompile]
        struct Pick : IJob
        {
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public int NumDynamicBodies;
            public NativeArray<SpringData> SpringData;
            public Ray Ray;
            public float Near;
            public float3 Forward;

            public void Execute()
            {
                float fraction = 1.0f;
                RigidBody? hitBody = null;

                var rayCastInput = new RaycastInput { Ray = Ray, Filter = CollisionFilter.Default };
                if (CollisionWorld.CastRay(rayCastInput, out RaycastHit hit))
                {
                    if (hit.RigidBodyIndex < NumDynamicBodies)
                    {
                        hitBody = CollisionWorld.Bodies[hit.RigidBodyIndex];
                        fraction = hit.Fraction;
                    }
                }

                // If there was a hit, set up the spring
                if (hitBody != null)
                {
                    float3 pointInWorld = (Ray.Origin + Ray.Direction * fraction);
                    MTransform bodyFromWorld = Inverse(new MTransform(hitBody.Value.WorldFromBody));
                    float3 pointOnBody = Mul(bodyFromWorld, pointInWorld);

                    SpringData[0] = new SpringData
                    {
                        Entity = hitBody.Value.Entity,
                        Dragging = 1,
                        PointOnBody = pointOnBody,
                        MouseDepth = Near + math.dot(math.normalize(Ray.Direction), Forward) * fraction * k_MaxDistance
                    };
                }
                else
                {
                    SpringData[0] = new SpringData
                    {
                        Dragging = 0
                    };
                }
            }
        }

        public MousePickSystem()
        {
            SpringDatas = new NativeArray<SpringData>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            SpringDatas[0] = new SpringData();
        }

        protected override void OnCreate()
        {
            m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_MouseGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(MousePick) }
            });
        }

        protected override void OnDestroyManager()
        {
            SpringDatas.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_MouseGroup.CalculateLength() == 0)
            {
                return inputDeps;
            }

            var handle = JobHandle.CombineDependencies(inputDeps, m_BuildPhysicsWorldSystem.FinalJobHandle);

            if (Input.GetMouseButtonDown(0) && (Camera.main != null))
            {
                Vector2 mousePosition = Input.mousePosition;
                UnityEngine.Ray unityRay = Camera.main.ScreenPointToRay(mousePosition);
                var ray = new Ray(unityRay.origin, unityRay.direction * k_MaxDistance);

                // Schedule picking job, after the collision world has been built
                handle = new Pick
                {
                    CollisionWorld = m_BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld,
                    NumDynamicBodies = m_BuildPhysicsWorldSystem.PhysicsWorld.NumDynamicBodies,
                    SpringData = SpringDatas,
                    Ray = ray,
                    Near = Camera.main.nearClipPlane,
                    Forward = Camera.main.transform.forward
                }.Schedule(JobHandle.CombineDependencies(handle, m_BuildPhysicsWorldSystem.FinalJobHandle));

                PickJobHandle = handle;

                handle.Complete(); // TODO.ma figure out how to do this properly...we need a way to make physics sync wait for
                // any user jobs that touch the component data, maybe a JobHandle LastUserJob or something that the user has to set
            }

            if (Input.GetMouseButtonUp(0))
            {
                SpringDatas[0] = new SpringData();
            }

            return handle;
        }
    }

    // Applies any mouse spring as a change in velocity on the entity's motion component
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    public class MouseSpringSystem : JobComponentSystem
    {
        EntityQuery m_MouseGroup;
        MousePickSystem m_PickSystem;

        protected override void OnCreate()
        {
            m_PickSystem = World.GetOrCreateSystem<MousePickSystem>();
            m_MouseGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(MousePick) }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_MouseGroup.CalculateLength() == 0)
            {
                return inputDeps;
            }

            ComponentDataFromEntity<Translation> Positions = GetComponentDataFromEntity<Translation>(true);
            ComponentDataFromEntity<Rotation> Rotations = GetComponentDataFromEntity<Rotation>(true);
            ComponentDataFromEntity<PhysicsVelocity> Velocities = GetComponentDataFromEntity<PhysicsVelocity>();
            ComponentDataFromEntity<PhysicsMass> Masses = GetComponentDataFromEntity<PhysicsMass>(true);

            // If there's a pick job, wait for it to finish
            if (m_PickSystem.PickJobHandle != null)
            {
                JobHandle.CombineDependencies(inputDeps, m_PickSystem.PickJobHandle.Value).Complete();
            }

            // If there's a picked entity, drag it
            MousePickSystem.SpringData springData = m_PickSystem.SpringDatas[0];
            if (springData.Dragging != 0)
            {
                Entity entity = m_PickSystem.SpringDatas[0].Entity;
                if (!EntityManager.HasComponent<PhysicsMass>(entity))
                {
                    return inputDeps;
                }

                PhysicsMass massComponent = Masses[entity];
                PhysicsVelocity velocityComponent = Velocities[entity];

                if (massComponent.InverseMass == 0)
                {
                    return inputDeps;
                }

                var worldFromBody = new MTransform(Rotations[entity].Value, Positions[entity].Value);

                // Body to motion transform
                var bodyFromMotion = new MTransform(Masses[entity].InertiaOrientation, Masses[entity].CenterOfMass);
                MTransform worldFromMotion = Mul(worldFromBody, bodyFromMotion);

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
                    deltaVelocity = -pointDiff * (elasticity / Time.fixedDeltaTime) - damping * relativeVelocityInWorld;
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
                float maxImpulse = math.rcp(massComponent.InverseMass) * Time.fixedDeltaTime * maxAcceleration;
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

            return inputDeps;
        }
    }
}
