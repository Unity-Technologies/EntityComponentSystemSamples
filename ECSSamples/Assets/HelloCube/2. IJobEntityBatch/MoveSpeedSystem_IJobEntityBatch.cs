using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public partial class MoveSpeedSystem_IJobEntityBatch : SystemBase
{
    EntityQuery m_Query;
    protected override void OnCreate()
    {
        m_Query = GetEntityQuery(typeof(Translation), ComponentType.ReadOnly<MoveSpeed_IJobEntityBatch>());
    }

    struct MoveSpeedJob : IJobEntityBatch
    {
        public float DeltaTime;
        public ComponentTypeHandle<Translation> TranslationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<MoveSpeed_IJobEntityBatch> MoveSpeedTypeHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkTranslation = batchInChunk.GetNativeArray(TranslationTypeHandle);
            var chunkMoveSpeeds = batchInChunk.GetNativeArray(MoveSpeedTypeHandle);

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var translation = chunkTranslation[i];
                var moveSpeed = chunkMoveSpeeds[i];

                chunkTranslation[i] = new Translation
                {
                    Value = translation.Value + (float3)moveSpeed.MoveSpeed * DeltaTime
                };
            }
        }
    }

    protected override void OnUpdate()
    {
        var translationType = GetComponentTypeHandle<Translation>();
        var moveSpeedType = GetComponentTypeHandle<MoveSpeed_IJobEntityBatch>();
        
        var job = new MoveSpeedJob()
        {
            TranslationTypeHandle = translationType,
            MoveSpeedTypeHandle = moveSpeedType,
            DeltaTime = Time.DeltaTime
        };

        Dependency = job.ScheduleParallel(m_Query, Dependency);
    }
}
