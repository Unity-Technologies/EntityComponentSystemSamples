using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Baking.AutoAuthoring.BakingTypeAutoAuthoring
{
    // Bake additional authoring properties that could not be processed in a baker.
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct PositionBakeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Complex>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (complex, transform) in
                     SystemAPI.Query<RefRO<Complex>, RefRW<LocalTransform>>()
                         .WithAll<BakedEntity>()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab))
            {
                transform.ValueRW = LocalTransform.FromPositionRotation(complex.ValueRO.Properties.Position,
                    quaternion.Euler(math.radians(complex.ValueRO.Properties.Rotation)));
            }
        }
    }
}
