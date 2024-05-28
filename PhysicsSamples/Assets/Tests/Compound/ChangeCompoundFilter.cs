using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Unity.Physics.Tests
{
    public struct ChangeCompoundFilterData : IComponentData {}

    public class ChangeCompoundFilterScene : SceneCreationSettings {}

    public class ChangeCompoundFilter : SceneCreationAuthoring<ChangeCompoundFilterScene>
    {
        class ChangeCompoundFilterBaker : Baker<ChangeCompoundFilter>
        {
            public override void Bake(ChangeCompoundFilter authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new ChangeCompoundFilterScene
                {
                    DynamicMaterial = authoring.DynamicMaterial,
                    StaticMaterial = authoring.StaticMaterial
                });
                AddComponent<ChangeCompoundFilterData>(entity);
            }
        }
    }

    public partial class ChangeCompoundFilterSystem : SceneCreationSystem<ChangeCompoundFilterScene>
    {
        private BlobAssetReference<Collider> CreateGroundCompoundWith2Children(float3 groundSize)
        {
            using var groundCollider1 = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
            using var groundCollider2 = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
            var instances = new NativeArray<CompoundCollider.ColliderBlobInstance>(2, Allocator.Temp);
            instances[0] = new CompoundCollider.ColliderBlobInstance
            {
                Collider = groundCollider1,
                CompoundFromChild = RigidTransform.identity
            };
            instances[1] = new CompoundCollider.ColliderBlobInstance
            {
                Collider = groundCollider2,
                CompoundFromChild = RigidTransform.identity
            };
            return CompoundCollider.Create(instances);
        }

        public override void CreateScene(ChangeCompoundFilterScene sceneSettings)
        {
            // Common params
            float3 groundSize = new float3(5.0f, 1.0f, 5.0f);
            float3 boxSize = new float3(1.0f, 1.0f, 1.0f);
            float mass = 1.0f;

            // Compound ground which will have child 0 change filter
            {
                var compoundCollider = CreateGroundCompoundWith2Children(groundSize);
                CreateStaticBody(new float3(-20.0f, 0.0f, 0.0f), quaternion.identity, compoundCollider);

                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(-20.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(compoundCollider);
                CreatedColliders.Add(boxCollider);
            }

            // Compound ground which will have child 1 change filter
            {
                var compoundCollider = CreateGroundCompoundWith2Children(groundSize);
                CreateStaticBody(new float3(-10.0f, 0.0f, 0.0f), quaternion.identity, compoundCollider);

                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(-10.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(compoundCollider);
                CreatedColliders.Add(boxCollider);
            }

            // Compound ground which will have both children change filter
            {
                var compoundCollider = CreateGroundCompoundWith2Children(groundSize);
                CreateStaticBody(new float3(0.0f, 0.0f, 0.0f), quaternion.identity, compoundCollider);

                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(0.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(compoundCollider);
                CreatedColliders.Add(boxCollider);
            }

            // Compound ground which will have its filter changed through root collider
            {
                var compoundCollider = CreateGroundCompoundWith2Children(groundSize);
                CreateStaticBody(new float3(10.0f, 0.0f, 0.0f), quaternion.identity, compoundCollider);

                var boxCollider = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = boxSize });
                CreateDynamicBody(new float3(10.0f, 1.0f, 0.0f), quaternion.identity, boxCollider, float3.zero, float3.zero, mass);

                CreatedColliders.Add(compoundCollider);
                CreatedColliders.Add(boxCollider);
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct UpdateFilterSystem : ISystem
    {
        private int m_Counter;
        EntityQuery m_VerificationGroup;

        public void OnCreate(ref SystemState state)
        {
            m_VerificationGroup = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ChangeCompoundFilterData) }
            });

            state.RequireForUpdate(m_VerificationGroup);

            m_Counter = 0;
        }

        public void OnUpdate(ref SystemState state)
        {
            m_Counter++;
            if (m_Counter == 10)
            {
                state.Dependency.Complete();

                var bpwData = state.EntityManager.GetComponentData<BuildPhysicsWorldData>(state.World.GetExistingSystem<BuildPhysicsWorld>());
                var staticEntities = bpwData.StaticEntityGroup.ToEntityArray(Allocator.TempJob);

                // Change filter of child 0 in compound
                unsafe
                {
                    var colliderComponent = state.EntityManager.GetComponentData<PhysicsCollider>(staticEntities[0]);

                    CompoundCollider* compoundCollider = (CompoundCollider*)colliderComponent.ColliderPtr;
                    compoundCollider->SetCollisionFilter(CollisionFilter.Zero, compoundCollider->ConvertChildIndexToColliderKey(0));
                    state.EntityManager.SetComponentData(staticEntities[0], colliderComponent);
                }

                // Change filter of child 1 in compound
                unsafe
                {
                    var colliderComponent = state.EntityManager.GetComponentData<PhysicsCollider>(staticEntities[1]);

                    CompoundCollider* compoundCollider = (CompoundCollider*)colliderComponent.ColliderPtr;
                    compoundCollider->SetCollisionFilter(CollisionFilter.Zero, compoundCollider->ConvertChildIndexToColliderKey(1));
                    state.EntityManager.SetComponentData(staticEntities[1], colliderComponent);
                }

                // Change filter of both children in compound
                unsafe
                {
                    var colliderComponent = state.EntityManager.GetComponentData<PhysicsCollider>(staticEntities[2]);

                    CompoundCollider* compoundCollider = (CompoundCollider*)colliderComponent.ColliderPtr;
                    compoundCollider->SetCollisionFilter(CollisionFilter.Zero, compoundCollider->ConvertChildIndexToColliderKey(0));
                    compoundCollider->SetCollisionFilter(CollisionFilter.Zero, compoundCollider->ConvertChildIndexToColliderKey(1));
                    state.EntityManager.SetComponentData(staticEntities[2], colliderComponent);
                }

                // Change filter of the compound itself
                {
                    var colliderComponent = state.EntityManager.GetComponentData<PhysicsCollider>(staticEntities[3]);
                    colliderComponent.Value.Value.SetCollisionFilter(CollisionFilter.Zero);
                    state.EntityManager.SetComponentData(staticEntities[3], colliderComponent);
                }

                staticEntities.Dispose();
            }
            else if (m_Counter == 50)
            {
                var bpwData = state.EntityManager.GetComponentData<BuildPhysicsWorldData>(state.World.GetExistingSystem<BuildPhysicsWorld>());
                var dynamicEntities = bpwData.DynamicEntityGroup.ToEntityArray(Allocator.TempJob);

                // First 2 boxes should stay still, while other 2 should fall through
                {
                    var transform1 = state.EntityManager.GetComponentData<LocalTransform>(dynamicEntities[0]);
                    Assert.IsTrue(transform1.Position.y > 0.99f, "Box started falling!");
                    var transform2 = state.EntityManager.GetComponentData<LocalTransform>(dynamicEntities[1]);
                    Assert.IsTrue(transform2.Position.y > 0.99f, "Box started falling!");
                    var transform3 = state.EntityManager.GetComponentData<LocalTransform>(dynamicEntities[2]);
                    Assert.IsTrue(transform3.Position.y < 0.9f, "Box didn't start falling!");
                    var transform4 = state.EntityManager.GetComponentData<LocalTransform>(dynamicEntities[3]);
                    Assert.IsTrue(transform4.Position.y < 0.9f, "Box didn't start falling!");
                }

                dynamicEntities.Dispose();
            }
        }
    }
}
