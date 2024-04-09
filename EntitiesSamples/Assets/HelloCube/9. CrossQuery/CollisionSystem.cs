using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace HelloCube.CrossQuery
{
    public partial struct CollisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteCrossQuery>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var boxQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, DefaultColor, URPMaterialPropertyBaseColor>().Build();

#if false
            // More complex solution, but it avoids creating temporary copies of the box components
            new CollisionJob
            {
                LocalTransformTypeHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(true),
                DefaultColorTypeHandle = SystemAPI.GetComponentTypeHandle<DefaultColor>(true),
                BaseColorTypeHandle = SystemAPI.GetComponentTypeHandle<URPMaterialPropertyBaseColor>(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                OtherChunks = boxQuery.ToArchetypeChunkArray(state.WorldUpdateAllocator)
            }.ScheduleParallel(boxQuery, state.Dependency).Complete();
#else
            // Simple solution, but it requires creating temporary copies of all box translations and entity IDs
            var boxTransforms = boxQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var boxEntities = boxQuery.ToEntityArray(Allocator.Temp);

            foreach (var (transform, defaultColor, color, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<DefaultColor>,
                             RefRW<URPMaterialPropertyBaseColor>>()
                         .WithEntityAccess())
            {
                // reset color of the box to its default
                color.ValueRW.Value = defaultColor.ValueRO.Value;

                // change the color if this box intersects another
                for (int i = 0; i < boxTransforms.Length; i++)
                {
                    var otherEnt = boxEntities[i];
                    var otherTrans = boxTransforms[i];

                    // A box should not intersect with itself, so we check if the other entity's id matches the current entity's id.
                    if (entity != otherEnt && math.distancesq(transform.ValueRO.Position, otherTrans.Position) < 1)
                    {
                        color.ValueRW.Value.y = 0.5f; // set green channel
                        break;
                    }
                }
            }
#endif
        }
    }

    [BurstCompile]
    public struct CollisionJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<LocalTransform> LocalTransformTypeHandle;
        [ReadOnly] public ComponentTypeHandle<DefaultColor> DefaultColorTypeHandle;
        public ComponentTypeHandle<URPMaterialPropertyBaseColor> BaseColorTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        [ReadOnly] public NativeArray<ArchetypeChunk> OtherChunks;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var transforms = chunk.GetNativeArray(ref LocalTransformTypeHandle);
            var defaultColors = chunk.GetNativeArray(ref DefaultColorTypeHandle);
            var baseColors = chunk.GetNativeArray(ref BaseColorTypeHandle);
            var entities = chunk.GetNativeArray(EntityTypeHandle);

            for (int i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                var baseColor = baseColors[i];
                var entity = entities[i];

                // reset to default color
                baseColor.Value = defaultColors[i].Value;

                for (int j = 0; j < OtherChunks.Length; j++)
                {
                    var otherChunk = OtherChunks[j];
                    var otherTranslations = otherChunk.GetNativeArray(ref LocalTransformTypeHandle);
                    var otherEntities = otherChunk.GetNativeArray(EntityTypeHandle);

                    for (int k = 0; k < otherChunk.Count; k++)
                    {
                        var otherTranslation = otherTranslations[k];
                        var otherEntity = otherEntities[k];

                        if (entity != otherEntity && math.distancesq(transform.Position, otherTranslation.Position) < 1)
                        {
                            baseColor.Value.y = 0.5f; // set green channel
                            break;
                        }
                    }
                }

                baseColors[i] = baseColor;
            }
        }
    }
}
