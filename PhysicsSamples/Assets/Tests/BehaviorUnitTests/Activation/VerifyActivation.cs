using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Extensions;
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

    public class VerifyActivationScene : SceneCreationSettings {}

    public class VerifyActivation : SceneCreationAuthoring<VerifyActivationScene>
    {
        public bool PureFilter = true;
        public bool Remove = true;
        public bool MotionChange = true;
        public bool Teleport = true;
        public bool ColliderChange = true;
        public bool NewCollider = true;

        class VerifyActivationBaker : Baker<VerifyActivation>
        {
            public override void Bake(VerifyActivation authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new VerifyActivationScene()
                {
                    DynamicMaterial = authoring.DynamicMaterial,
                    StaticMaterial = authoring.StaticMaterial
                });
                AddComponent(entity, new VerifyActivationData
                {
                    PureFilter = authoring.PureFilter ? 1 : 0,
                    Remove = authoring.Remove ? 1 : 0,
                    MotionChange = authoring.MotionChange ? 1 : 0,
                    Teleport = authoring.Teleport ? 1 : 0,
                    ColliderChange = authoring.ColliderChange ? 1 : 0,
                    NewCollider = authoring.NewCollider ? 1 : 0
                });
            }
        }
    }

    public partial class VerifyActivationSystem : SceneCreationSystem<VerifyActivationScene>
    {
        public override void CreateScene(VerifyActivationScene sceneSettings)
        {
            // Common params
            float3 groundSize = new float3(5.0f, 1.0f, 5.0f);
            float3 boxSize = new float3(1.0f, 1.0f, 1.0f);
            float mass = 1.0f;

            Entity e = SystemAPI.ManagedAPI.GetSingletonEntity<VerifyActivationScene>();
            VerifyActivationData data = SystemAPI.GetComponent<VerifyActivationData>(e);

            // Ground to do nothing on (other than filter change) and dynamic box over it
            if (data.PureFilter == 1)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(-30.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(-30.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(groundCollider);
                CreatedColliders.Add(boxCollider);
            }

            // Ground to remove and dynamic box over it
            if (data.Remove == 1)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(-20.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(-20.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(groundCollider);
                CreatedColliders.Add(boxCollider);
            }

            // Ground to convert to dynamic and dynamic box over it
            if (data.MotionChange == 1)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(-10.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(-10.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(groundCollider);
                CreatedColliders.Add(boxCollider);
            }

            // Ground to teleport and dynamic box over it
            if (data.Teleport == 1)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(0.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(0.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(groundCollider);
                CreatedColliders.Add(boxCollider);
            }

            // Ground to change collider of and dynamic box over it
            if (data.ColliderChange == 1)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(10.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(10.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(groundCollider);
                CreatedColliders.Add(boxCollider);
            }

            // Ground to set new collider on and dynamic box over it
            if (data.NewCollider == 1)
            {
                var groundCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
                CreateStaticBody(new float3(20.0f, 0.0f, 0.0f), quaternion.identity, groundCollider);
                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(20.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(groundCollider);
                CreatedColliders.Add(boxCollider);
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct TestSystem : ISystem
    {
        private int m_Counter;

        EntityQuery m_VerificationGroup;
        ComponentLookup<VerifyActivationData> m_ActivationData;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_VerificationGroup = state.GetEntityQuery(ComponentType.ReadWrite<VerifyActivationData>());
            state.RequireForUpdate(m_VerificationGroup);
            m_Counter = 0;

            m_ActivationData = state.GetComponentLookup<VerifyActivationData>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            HandleUpdate(ref state);
        }

        internal void HandleUpdate(ref SystemState state)
        {
            m_Counter++;
            m_ActivationData.Update(ref state);
            if (m_Counter == 30)
            {
                VerifyActivationSystem system = state.World.GetExistingSystemManaged<VerifyActivationSystem>();

                // First change filter of all ground colliders to collide with nothing
                var bpwData = state.EntityManager.GetComponentData<BuildPhysicsWorldData>(state.World.GetExistingSystem<BuildPhysicsWorld>());
                var staticEntities = bpwData.StaticEntityGroup.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < staticEntities.Length; i++)
                {
                    var colliderComponent = state.EntityManager.GetComponentData<PhysicsCollider>(staticEntities[i]);
                    colliderComponent.Value.Value.SetCollisionFilter(new CollisionFilter
                    {
                        BelongsTo = ~CollisionFilter.Default.BelongsTo,
                        CollidesWith = ~CollisionFilter.Default.CollidesWith,
                        GroupIndex = 1
                    });
                    state.EntityManager.SetComponentData(staticEntities[i], colliderComponent);
                }

                var verificationData = m_VerificationGroup.ToEntityArray(Allocator.TempJob);
                var verificationComponentData = m_ActivationData[verificationData[0]];

                // Do nothing for ground 0 (other than filter change)
                int counter = 0;
                if (verificationComponentData.PureFilter > 0)
                {
                    counter++;
                }

                // Completely remove one ground (1)
                if (verificationComponentData.Remove > 0)
                {
                    state.EntityManager.DestroyEntity(staticEntities[counter]);
                    counter++;
                }

                // Convert one ground to dynamic object (2)
                if (verificationComponentData.MotionChange > 0)
                {
                    var colliderComponent = state.EntityManager.GetComponentData<PhysicsCollider>(staticEntities[counter]);
                    state.EntityManager.AddComponentData(staticEntities[counter], PhysicsMass.CreateDynamic(colliderComponent.MassProperties, 1.0f));
                    state.EntityManager.AddComponentData(staticEntities[counter], new PhysicsVelocity
                    {
                        Linear = new float3(0.0f, -1.0f, 0.0f),
                        Angular = float3.zero
                    });
                    counter++;
                }

                // Teleport one ground (3)
                if (verificationComponentData.Teleport > 0)
                {

                    var localTransformComponent = state.EntityManager.GetComponentData<LocalTransform>(staticEntities[counter]);
                    localTransformComponent.Position.y = -10.0f;
                    state.EntityManager.SetComponentData(staticEntities[counter], localTransformComponent);

                    counter++;
                }

                // Change collider of one ground (4)
                if (verificationComponentData.ColliderChange > 0)
                {
                    var colliderComponent = state.EntityManager.GetComponentData<PhysicsCollider>(staticEntities[counter]);
                    var oldFilter = colliderComponent.Value.Value.GetCollisionFilter();
                    colliderComponent.Value = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = new float3(5.0f, 1.0f, 50.0f) });
                    colliderComponent.Value.Value.SetCollisionFilter(oldFilter);
                    system.CreatedColliders.Add(colliderComponent.Value);
                    state.EntityManager.SetComponentData(staticEntities[counter], colliderComponent);
                    counter++;
                }

                // Set new collider of one ground (5)
                if (verificationComponentData.NewCollider > 0)
                {
                    var colliderComponent = state.EntityManager.GetComponentData<PhysicsCollider>(staticEntities[counter]);
                    var newColliderComponent = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = new float3(5.0f, 1.0f, 50.0f) });
                    system.CreatedColliders.Add(newColliderComponent);
                    newColliderComponent.Value.SetCollisionFilter(colliderComponent.Value.Value.GetCollisionFilter());
                    state.EntityManager.SetComponentData(staticEntities[counter], newColliderComponent.AsComponent());
                    counter++;
                }

                verificationData.Dispose();
                staticEntities.Dispose();
            }
            else if (m_Counter == 40)
            {
                // Verify that all boxes started falling after the ground was changed
                var bpwData = state.EntityManager.GetComponentData<BuildPhysicsWorldData>(state.World.GetExistingSystem<BuildPhysicsWorld>());
                var dynamicEntities = bpwData.DynamicEntityGroup.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < dynamicEntities.Length; i++)
                {

                    var localTransform = state.EntityManager.GetComponentData<LocalTransform>(dynamicEntities[i]);
                    Assert.IsTrue(localTransform.Position.y < 0.99f, "Box didn't start falling!");

                }

                dynamicEntities.Dispose();
            }
        }
    }
}
