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
        public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            base.Convert(entity, dstManager, conversionSystem);
            dstManager.AddComponentData(entity, new ChangeCompoundFilterData());
        }
    }

    public class ChangeCompoundFilterSystem : SceneCreationSystem<ChangeCompoundFilterScene>
    {
        private BlobAssetReference<Collider> CreateGroundCompoundWith2Children(float3 groundSize)
        {
            var groundCollider1 = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
            var groundCollider2 = BoxCollider.Create(new BoxGeometry { Orientation = quaternion.identity, Size = groundSize });
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
    [UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class UpdateFilterSystem : SystemBase
    {
        private int m_Counter;

        EntityQuery m_VerificationGroup;

        BuildPhysicsWorld m_BuildPhysicsWorld;
        ExportPhysicsWorld m_ExportPhysicsWorld;
        ChangeCompoundFilterSystem m_ChangeCompoundFilterSystem;

        protected override void OnCreate()
        {
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ChangeCompoundFilterData) }
            });

            RequireForUpdate(m_VerificationGroup);

            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_ExportPhysicsWorld = World.GetOrCreateSystem<ExportPhysicsWorld>();
            m_ChangeCompoundFilterSystem = World.GetOrCreateSystem<ChangeCompoundFilterSystem>();

            m_Counter = 0;
        }

        protected override void OnUpdate()
        {
            m_Counter++;
            if (m_Counter == 10)
            {
                m_ExportPhysicsWorld.GetOutputDependency().Complete();

                var staticEntities = m_BuildPhysicsWorld.StaticEntityGroup.ToEntityArray(Allocator.TempJob);

                // Change filter of child 0 in compound
                unsafe
                {
                    var colliderComponent = EntityManager.GetComponentData<PhysicsCollider>(staticEntities[0]);

                    ColliderKey colliderKey0 = ColliderKey.Empty;
                    colliderKey0.PushSubKey(colliderComponent.Value.Value.NumColliderKeyBits, 0);

                    CompoundCollider* compoundCollider = (CompoundCollider*)colliderComponent.ColliderPtr;
                    compoundCollider->GetChild(ref colliderKey0, out ChildCollider child0);
                    child0.Collider->Filter = CollisionFilter.Zero;
                    compoundCollider->RefreshCollisionFilter();

                    EntityManager.SetComponentData(staticEntities[0], colliderComponent);
                }

                // Change filter of child 1 in compound
                unsafe
                {
                    var colliderComponent = EntityManager.GetComponentData<PhysicsCollider>(staticEntities[1]);

                    ColliderKey colliderKey1 = ColliderKey.Empty;
                    colliderKey1.PushSubKey(colliderComponent.Value.Value.NumColliderKeyBits, 1);

                    CompoundCollider* compoundCollider = (CompoundCollider*)colliderComponent.ColliderPtr;
                    compoundCollider->GetChild(ref colliderKey1, out ChildCollider child1);
                    child1.Collider->Filter = CollisionFilter.Zero;
                    compoundCollider->RefreshCollisionFilter();

                    EntityManager.SetComponentData(staticEntities[1], colliderComponent);
                }

                // Change filter of both children in compound
                unsafe
                {
                    var colliderComponent = EntityManager.GetComponentData<PhysicsCollider>(staticEntities[2]);

                    ColliderKey colliderKey0 = ColliderKey.Empty;
                    colliderKey0.PushSubKey(colliderComponent.Value.Value.NumColliderKeyBits, 0);
                    ColliderKey colliderKey1 = ColliderKey.Empty;
                    colliderKey1.PushSubKey(colliderComponent.Value.Value.NumColliderKeyBits, 1);

                    CompoundCollider* compoundCollider = (CompoundCollider*)colliderComponent.ColliderPtr;
                    compoundCollider->GetChild(ref colliderKey0, out ChildCollider child0);
                    compoundCollider->GetChild(ref colliderKey1, out ChildCollider child1);
                    child0.Collider->Filter = CollisionFilter.Zero;
                    child1.Collider->Filter = CollisionFilter.Zero;
                    compoundCollider->RefreshCollisionFilter();

                    EntityManager.SetComponentData(staticEntities[2], colliderComponent);
                }

                // Change filter of the compound itself
                {
                    var colliderComponent = EntityManager.GetComponentData<PhysicsCollider>(staticEntities[3]);
                    colliderComponent.Value.Value.Filter = CollisionFilter.Zero;
                    EntityManager.SetComponentData(staticEntities[3], colliderComponent);
                }

                staticEntities.Dispose();
            }
            else if (m_Counter == 50)
            {
                var dynamicEntities = m_BuildPhysicsWorld.DynamicEntityGroup.ToEntityArray(Allocator.TempJob);

                // First 2 boxes should stay still, while other 2 should fall through
                {
                    var translation1 = EntityManager.GetComponentData<Translation>(dynamicEntities[0]);
                    Assert.IsTrue(translation1.Value.y > 0.99f, "Box started falling!");
                    var translation2 = EntityManager.GetComponentData<Translation>(dynamicEntities[1]);
                    Assert.IsTrue(translation2.Value.y > 0.99f, "Box started falling!");
                    var translation3 = EntityManager.GetComponentData<Translation>(dynamicEntities[2]);
                    Assert.IsTrue(translation3.Value.y < 0.9f, "Box didn't start falling!");
                    var translation4 = EntityManager.GetComponentData<Translation>(dynamicEntities[3]);
                    Assert.IsTrue(translation4.Value.y < 0.9f, "Box didn't start falling!");
                }

                dynamicEntities.Dispose();
            }
        }
    }
}
