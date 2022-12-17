using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.JobChunk
{
    [BurstCompile]
    public partial struct RotationSpeedSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Will be cached in OnCreate for you
            var spinningCubesQuery = SystemAPI.QueryBuilder().WithAll<RotationSpeed, LocalTransform>().Build();

            // SystemAPI is able to cache TypeHandles for you inside OnCreate, and will call .Update at the call site.
            // This is more efficient than calling state.GetComponentTypeHandle() every frame.
            var transformTypeHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>();
            var rotationSpeedTypeHandle = SystemAPI.GetComponentTypeHandle<RotationSpeed>(true);

            var job = new RotationSpeedJob
            {
                TransformTypeHandle = transformTypeHandle,
                RotationSpeedTypeHandle = rotationSpeedTypeHandle,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            // Unlike an IJobEntity, an IJobChunk must be manually passed a query.
            // Also an unlike IJobEntity, IJobChunk does not pass and assign the state.Dependency JobHandle implicitly.
            // Using state.Dependency correctly ensures that the entity jobs scheduled in different systems will
            // depend upon each other where necessary.
            state.Dependency = job.ScheduleParallel(spinningCubesQuery, state.Dependency);
        }
    }

    [BurstCompile]
    struct RotationSpeedJob : IJobChunk
    {
        public float DeltaTime;
        public ComponentTypeHandle<LocalTransform> TransformTypeHandle;
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

            var chunkTransforms = chunk.GetNativeArray(ref TransformTypeHandle);
            var chunkRotationSpeeds = chunk.GetNativeArray(ref RotationSpeedTypeHandle);
            for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
            {
                var rotationSpeed = chunkRotationSpeeds[i];
                chunkTransforms[i] = chunkTransforms[i].RotateY(rotationSpeed.RadiansPerSecond * DeltaTime);
            }
        }
    }
}
