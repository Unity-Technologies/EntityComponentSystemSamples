using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;

namespace Modify
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(EndColliderBakingSystem))]
    public partial struct ColliderTypeBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var manager = state.EntityManager;
            foreach (var colliderType in
                     SystemAPI.Query<RefRW<ChangeColliderType>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                var colliderA = manager.GetComponentData<PhysicsCollider>(colliderType.ValueRW.EntityA);
                var colliderB = manager.GetComponentData<PhysicsCollider>(colliderType.ValueRW.EntityB);

                colliderType.ValueRW.ColliderA = colliderA;
                colliderType.ValueRW.ColliderB = colliderB;
            }
        }
    }
}
