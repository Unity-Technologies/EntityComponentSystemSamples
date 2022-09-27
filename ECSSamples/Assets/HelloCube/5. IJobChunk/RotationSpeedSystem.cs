#if !ENABLE_TRANSFORM_V1
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.JobChunk
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(JobChunkGroup))]
    [BurstCompile]
    public partial struct RotationSpeedSystem : ISystem
    {
        EntityQuery m_SpinningCubes;
        ComponentTypeHandle<LocalToWorldTransform> m_TransformTypeHandle;
        ComponentTypeHandle<RotationSpeed> m_RotationSpeedTypeHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var queryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<RotationSpeed, LocalToWorldTransform>();
            m_SpinningCubes = state.GetEntityQuery(queryBuilder);

            m_TransformTypeHandle = state.GetComponentTypeHandle<LocalToWorldTransform>();
            m_RotationSpeedTypeHandle = state.GetComponentTypeHandle<RotationSpeed>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // The component type handles needed by the job are stored in fields but then updated every frame.
            // This is more efficient than calling GetComponentTypeHandle() every frame.
            m_TransformTypeHandle.Update(ref state);
            m_RotationSpeedTypeHandle.Update(ref state);

            var job = new RotationSpeedJob
            {
                TransformTypeHandle = m_TransformTypeHandle,
                RotationSpeedTypeHandle = m_RotationSpeedTypeHandle,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            // Unlike an IJobEntity, an IJobChunk must be manually passed a query.
            // Also an unlike IJobEntity, IJobChunk does not pass and assign the state.Dependency JobHandle implicitly.
            // Using state.Dependency correctly ensures that the entity jobs scheduled in different systems will
            // depend upon each other where necessary.
            state.Dependency = job.ScheduleParallel(m_SpinningCubes, state.Dependency);
        }
    }

    [BurstCompile]
    struct RotationSpeedJob : IJobChunk
    {
        public float DeltaTime;
        public ComponentTypeHandle<LocalToWorldTransform> TransformTypeHandle;
        [ReadOnly] public ComponentTypeHandle<RotationSpeed> RotationSpeedTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            // The useEnableMask parameter is true when one or more entities in
            // the chunk have components of the query that are disabled.
            // If none of the query component types implement IEnableableComponent,
            // we can assume that useEnabledMask will always be false.
            // However, it's good practice to add this guard check just in case
            // someone later changes the query or component types.
            Assert.IsFalse(useEnabledMask);

            var chunkTransforms = chunk.GetNativeArray(TransformTypeHandle);
            var chunkRotationSpeeds = chunk.GetNativeArray(RotationSpeedTypeHandle);
            for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
            {
                var rotationSpeed = chunkRotationSpeeds[i];

                chunkTransforms[i] = new LocalToWorldTransform
                {
                    Value = chunkTransforms[i].Value.RotateY(rotationSpeed.RadiansPerSecond * DeltaTime)
                };
            }
        }
    }
}
#endif
