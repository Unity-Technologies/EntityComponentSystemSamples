// This script is used in the 5g1. Change Collision Filter - Boxes demo. This script instantiates a
// 3x3 grid of 9 prefab boxes with unique colliders. 3 boxes are created from a PhysicsShape with the
// Force Unique toggle enabled, 3 boxes are created from a PhysicsShape with the Force Unique toggle
// disabled with an added Force Unique Component added, and the last 3 boxes are created from a BoxCollider
// with the Force Unique Component added.
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Physics
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ColliderGridCreationSystem : SystemBase
    {
        private EntityQuery m_ColliderQuery;

        protected override void OnCreate()
        {
            RequireForUpdate<CreateColliderGridComponent>();
            m_ColliderQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(CreateColliderGridComponent),
                },
            });
        }

        protected override void OnStartRunning()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entityManager = World.EntityManager;

            var entity = m_ColliderQuery.ToEntityArray(Allocator.TempJob);

            // Grab data from the baked authoring component
            var data = entityManager.GetComponentData<CreateColliderGridComponent>(entity[0]);
            var prefabBuiltIn = data.BuiltInEntity;
            var prefabPhysicsShapeToggle = data.PhysicsShapeToggleEntity;
            var prefabPhysicsShapeComponent = data.PhysicsShapeComponentEntity;
            var startingPosition = data.SpawningPosition;

            var entitiesPhysicsShapeComponent = new NativeArray<Entity>(3, Allocator.Temp);
            var entitiesPhysicsShapeToggle = new NativeArray<Entity>(3, Allocator.Temp);
            var entitiesBuiltIn = new NativeArray<Entity>(3, Allocator.Temp);

            entityManager.Instantiate(prefabPhysicsShapeComponent, entitiesPhysicsShapeComponent);
            entityManager.Instantiate(prefabPhysicsShapeToggle, entitiesPhysicsShapeToggle);
            entityManager.Instantiate(prefabBuiltIn, entitiesBuiltIn);

            // Instantiate the colliders using a Physics Shape and a Force Unique Component
            var verticalOffset = 5f;
            var position = startingPosition + new float3(0f, verticalOffset, -5f);
            foreach (var e in entitiesPhysicsShapeComponent)
            {
                ecb.SetComponent(e, new LocalTransform
                {
                    Position = position,
                    Scale = 1,
                    Rotation = quaternion.identity
                });
                position += new float3(0f, 0f, 5f);
            }

            // Instantiate the colliders using a Physics Shape with the Force Unique toggle enabled
            position = startingPosition + new float3(-3.53f, verticalOffset, -5f);
            foreach (var e in entitiesPhysicsShapeToggle)
            {
                ecb.SetComponent(e, new LocalTransform
                {
                    Position = position,
                    Scale = 1,
                    Rotation = quaternion.identity
                });
                position += new float3(0f, 0f, 5f);
            }

            // Instantiate the colliders using a Built-In Collider with a Force Unique Component
            position = startingPosition + new float3(3.53f, verticalOffset, -5f);
            foreach (var e in entitiesBuiltIn)
            {
                ecb.SetComponent(e, new LocalTransform
                {
                    Position = position,
                    Scale = 1,
                    Rotation = quaternion.identity
                });
                position += new float3(0f, 0f, 5f);
            }

            entitiesPhysicsShapeComponent.Dispose();
            entitiesPhysicsShapeToggle.Dispose();
            entitiesBuiltIn.Dispose();
            entity.Dispose();

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        protected override void OnUpdate() {}

        protected override void OnDestroy() {}
    }
}
