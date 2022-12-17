using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace CrossQuery
{

#if true
// Simple solution, but it requires creating temporary copies of all box translations and entity IDs
[BurstCompile]
public partial struct CollisionSystem : ISystem
{
    private EntityQuery boxQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<LocalTransform, DefaultColor, URPMaterialPropertyBaseColor>();
        boxQuery = state.GetEntityQuery(builder);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;

        var boxTransforms = boxQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var boxEntities = boxQuery.ToEntityArray(Allocator.Temp);

        foreach (var (transform, defaultColor, color, entity) 
                 in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DefaultColor>, RefRW<URPMaterialPropertyBaseColor>>().WithEntityAccess())
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
                    color.ValueRW.Value.y = 0.5f;   // set green channel
                    break;
                }
            }
        }
    }
}
#else
    // More complex solution, but it avoids creating temporary copies of the box components
    [BurstCompile]
    public partial struct CollisionSystem : ISystem
    {
        EntityQuery boxQuery;

        ComponentTypeHandle<LocalToWorldTransform> _localToWorldTransformHandle;
        ComponentTypeHandle<DefaultColor> _defaultColorHandle;
        ComponentTypeHandle<URPMaterialPropertyBaseColor> _baseColorHandle;
        EntityTypeHandle _entityHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);
            builder.WithAll<LocalToWorldTransform, DefaultColor, URPMaterialPropertyBaseColor>();
            boxQuery = state.GetEntityQuery(builder);

            _localToWorldTransformHandle = state.GetComponentTypeHandle<LocalToWorldTransform>(true);
            _defaultColorHandle = state.GetComponentTypeHandle<DefaultColor>(true);
            _baseColorHandle = state.GetComponentTypeHandle<URPMaterialPropertyBaseColor>();
            _entityHandle = state.GetEntityTypeHandle();
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localToWorldTransformHandle.Update(ref state);
            _defaultColorHandle.Update(ref state);
            _baseColorHandle.Update(ref state);
            _entityHandle.Update(ref state);
            
            var chunks = boxQuery.ToArchetypeChunkArray(state.WorldUpdateAllocator);
            var collisionJob = new CollisionJob
            {
                LocalToWorldTransformTypeHandle = _localToWorldTransformHandle,
                DefaultColorTypeHandle = _defaultColorHandle,
                BaseColorTypeHandle = _baseColorHandle,
                EntityTypeHandle = _entityHandle,
                OtherChunks = chunks
            };

            var jobHandle = collisionJob.ScheduleParallel(boxQuery, state.Dependency);
            jobHandle.Complete();
        }
    }
    
    public struct CollisionJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<LocalToWorldTransform> LocalToWorldTransformTypeHandle;
        [ReadOnly] public ComponentTypeHandle<DefaultColor> DefaultColorTypeHandle;
        public ComponentTypeHandle<URPMaterialPropertyBaseColor> BaseColorTypeHandle;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;

        [ReadOnly] public NativeArray<ArchetypeChunk> OtherChunks;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var transforms = chunk.GetNativeArray(LocalToWorldTransformTypeHandle);
            var defaultColors = chunk.GetNativeArray(DefaultColorTypeHandle);
            var baseColors = chunk.GetNativeArray(BaseColorTypeHandle);
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
                    var otherTranslations = otherChunk.GetNativeArray(LocalToWorldTransformTypeHandle);
                    var otherEntities = otherChunk.GetNativeArray(EntityTypeHandle);

                    for (int k = 0; k < otherChunk.Count; k++)
                    {
                        var otherTranslation = otherTranslations[k];
                        var otherEntity = otherEntities[k];

                        if (entity != otherEntity && math.distancesq(transform.Value.Position, otherTranslation.Value.Position) < 1)
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
#endif
}