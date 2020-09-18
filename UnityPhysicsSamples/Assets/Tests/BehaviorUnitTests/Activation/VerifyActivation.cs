using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Unity.Physics.Tests
{
    public struct VerifyActivationData : IComponentData
    {
        public int PureFilter;
        public int Remove;
        public int MotionChange;
        public int Teleport;
        public int ColliderChange;
        public int NewCollider;
    }

    public class VerifyActivation : BasePhysicsDemo, IConvertGameObjectToEntity
    {
        public bool PureFilter = true;
        public bool Remove = true;
        public bool MotionChange = true;
        public bool Teleport = true;
        public bool ColliderChange = true;
        public bool NewCollider = true;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyActivationData
            {
                PureFilter = PureFilter ? 1 : 0,
                Remove = Remove ? 1 : 0,
                MotionChange = MotionChange ? 1 : 0,
                Teleport = Teleport ? 1 : 0,
                ColliderChange = ColliderChange ? 1 : 0,
                NewCollider = NewCollider ? 1 : 0
            });
        }

        protected override void Start()
        {
            base.init();

            // Common params
            float3 groundSize = new float3(5.0f, 1.0f, 5.0f);
            float3 boxSize = new float3(1.0f, 1.0f, 1.0f);
            float mass = 1.0f;

            // Ground to do nothing on (other than filter change) and dynamic box over it
            if (PureFilter)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(-30.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(-30.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);
            }

            // Ground to remove and dynamic box over it
            if (Remove)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(-20.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(-20.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);
            }

            // Ground to convert to dynamic and dynamic box over it
            if (MotionChange)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(-10.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(-10.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);
            }

            // Ground to teleport and dynamic box over it
            if (Teleport)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(0.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(0.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);
            }

            // Ground to change collider of and dynamic box over it
            if (ColliderChange)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(10.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(10.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);
            }

            // Ground to set new collider on and dynamic box over it
            if (NewCollider)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(20.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(20.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class TestSystem : SystemBase
    {
        private int m_Counter;

        EntityQuery m_VerificationGroup;

        BuildPhysicsWorld m_BuildPhysicsWorld;

        protected override void OnCreate()
        {
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyActivationData) }
            });

            RequireForUpdate(m_VerificationGroup);

            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

            m_Counter = 0;
        }

        protected unsafe override void OnUpdate()
        {
            m_Counter++;
            if (m_Counter == 30)
            {
                // First change filter of all ground colliders to collide with nothing
                var staticEntities = m_BuildPhysicsWorld.StaticEntityGroup.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < staticEntities.Length; i++)
                {
                    var colliderComponent = EntityManager.GetComponentData<PhysicsCollider>(staticEntities[i]);
                    colliderComponent.Value.Value.Filter = new CollisionFilter
                    {
                        BelongsTo = ~CollisionFilter.Default.BelongsTo,
                        CollidesWith = ~CollisionFilter.Default.CollidesWith,
                        GroupIndex = 1
                    };
                }

                var verificationData = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
                var verificationComponentData = GetComponentDataFromEntity<VerifyActivationData>(true)[verificationData[0]];

                // Do nothing for ground 0 (other than filter change)
                int counter = 0;
                if (verificationComponentData.PureFilter > 0)
                {
                    counter++;
                }

                // Completely remove one ground (1)
                if (verificationComponentData.Remove > 0)
                {
                    EntityManager.DestroyEntity(staticEntities[counter]);
                    counter++;
                }

                // Convert one ground to dynamic object (2)
                if (verificationComponentData.MotionChange > 0)
                {
                    var colliderComponent = EntityManager.GetComponentData<PhysicsCollider>(staticEntities[counter]);
                    EntityManager.AddComponentData(staticEntities[counter], PhysicsMass.CreateDynamic(colliderComponent.MassProperties, 1.0f));
                    EntityManager.AddComponentData(staticEntities[counter], new PhysicsVelocity
                    {
                        Linear = new float3(0.0f, -1.0f, 0.0f),
                        Angular = float3.zero
                    });
                    counter++;
                }

                // Teleport one ground (3)
                if (verificationComponentData.Teleport > 0)
                {
                    var translationComponent = EntityManager.GetComponentData<Translation>(staticEntities[counter]);
                    translationComponent.Value.y = -10.0f;
                    EntityManager.SetComponentData(staticEntities[counter], translationComponent);
                    counter++;
                }

                // Change collider of one ground (4)
                if (verificationComponentData.ColliderChange > 0)
                {
                    var colliderComponent = EntityManager.GetComponentData<PhysicsCollider>(staticEntities[counter]);
                    colliderComponent.Value = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = new float3(5.0f, 1.0f, 50.0f) });
                    counter++;
                }

                // Set new collider of one ground (5)
                if (verificationComponentData.NewCollider > 0)
                {
                    var colliderComponent = EntityManager.GetComponentData<PhysicsCollider>(staticEntities[counter]);
                    var newColliderComponent = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = new float3(5.0f, 1.0f, 50.0f) });
                    newColliderComponent.Value.Filter = colliderComponent.Value.Value.Filter;
                    EntityManager.SetComponentData(staticEntities[counter], new PhysicsCollider { Value = newColliderComponent });
                    counter++;
                }

                verificationData.Dispose();
                staticEntities.Dispose();
            }
            else if (m_Counter == 40)
            {
                // Verify that all boxes started falling after the ground was changed
                var dynamicEntities = m_BuildPhysicsWorld.DynamicEntityGroup.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < dynamicEntities.Length; i++)
                {
                    var translation = EntityManager.GetComponentData<Translation>(dynamicEntities[i]);
                    Assert.IsTrue(translation.Value.y < 0.99f, "Box didn't start falling!");
                }

                dynamicEntities.Dispose();
            }
        }
    }
}
