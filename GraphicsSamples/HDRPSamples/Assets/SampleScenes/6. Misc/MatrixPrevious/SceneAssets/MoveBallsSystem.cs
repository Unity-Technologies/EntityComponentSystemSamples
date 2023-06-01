using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.PlayerLoop;

[Serializable]
public struct BallOriginalTranslation : IComponentData
{
    public float3 Value;
}

[RequireMatchingQueriesForUpdate]
public partial class MoveBallsSystem : SystemBase
{
    private EntityQuery m_Group;

    [BurstCompile]
    struct MoveBall : IJobChunk
    {
        public ComponentTypeHandle<LocalTransform> LocalTransformType;
        [ReadOnly] public ComponentTypeHandle<BallOriginalTranslation> BallOriginalTranslationType;
        public uint LastSystemVersion;
        public double ElapsedTime;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // This job is not written to support queries with enableable component types.
            Assert.IsFalse(useEnabledMask);

            var chunkLocalTransform = chunk.GetNativeArray(ref LocalTransformType);
            var chunkOrigTranslation = chunk.GetNativeArray(ref BallOriginalTranslationType);

            for (int i = 0; i < chunk.Count; i++)
            {
                chunkLocalTransform[i] = LocalTransform.FromPosition(
                    chunkOrigTranslation[i].Value + (new float3(
                          (float)Math.Cos(ElapsedTime * 10.0f) * 0.7f,
                          (float)Math.Sin(ElapsedTime * 10.0f) * 0.7f,
                          0.0f)));
            }
        }
    }

    protected override void OnCreate()
    {
        m_Group = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(LocalTransform),
                typeof(BallOriginalTranslation)
            },
            Options = EntityQueryOptions.FilterWriteGroup
        });
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer entityOriginsCommandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback );
        Entities.WithNone<BallOriginalTranslation>().ForEach((Entity entity, in LocalTransform localTransform, in SphereId sphereId) =>
        {
            entityOriginsCommandBuffer.AddComponent(entity, new BallOriginalTranslation{ Value = localTransform.Position });
        }).Run();
        entityOriginsCommandBuffer.Playback(EntityManager);
        entityOriginsCommandBuffer.Dispose();

        var moveBallJob = new MoveBall
        {
            LocalTransformType = GetComponentTypeHandle<LocalTransform>(),
            BallOriginalTranslationType = GetComponentTypeHandle<BallOriginalTranslation>(true),
            LastSystemVersion = LastSystemVersion,
            ElapsedTime = SystemAPI.Time.ElapsedTime
        };
        Dependency = moveBallJob.ScheduleParallel(m_Group, Dependency);
    }
}
