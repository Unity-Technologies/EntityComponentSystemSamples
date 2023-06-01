using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using UnityEngine;

namespace Unity.Physics.Extensions
{
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

        public MousePickSystem()
        {
            SpringDataRef =
                new NativeReference<SpringData>(Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
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
                var mousePickCollector =
                    new MousePickCollector(1.0f, CollisionWorld.Bodies, CollisionWorld.NumDynamicBodies);
                mousePickCollector.IgnoreTriggers = IgnoreTriggers;

                if (CollisionWorld.CastRay(RayInput, ref mousePickCollector))
                {
                    float fraction = mousePickCollector.Hit.Fraction;
                    RigidBody hitBody = CollisionWorld.Bodies[mousePickCollector.Hit.RigidBodyIndex];

                    Math.MTransform bodyFromWorld = Math.Inverse(new Math.MTransform(hitBody.WorldFromBody));
                    float3 pointOnBody = Math.Mul(bodyFromWorld, mousePickCollector.Hit.Position);

                    SpringDataRef.Value = new SpringData
                    {
                        Entity = hitBody.Entity,
                        Dragging = true,
                        PointOnBody = pointOnBody,
                        MouseDepth = Near + math.dot(math.normalize(RayInput.End - RayInput.Start), Forward) *
                            fraction * k_MaxDistance,
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
    }

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
                isAcceptable = isAcceptable &&
                    hit.Material.CollisionResponse != CollisionResponsePolicy.RaiseTriggerEvents;
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
}
