using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.JobChunk
{
    public partial struct RotationSystem : ISystem
    {
        private ComponentTypeHandle<LocalTransform> transformTypeHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            transformTypeHandle = state.GetComponentTypeHandle<LocalTransform>();
            state.RequireForUpdate<Execute.IJobChunk>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var spinningCubesQuery = SystemAPI.QueryBuilder().WithAll<RotationSpeed, LocalTransform>().Build();

            // Because we cached the TypeHandle in OnCreate, we have to Update it each frame before use.
            transformTypeHandle.Update(ref state);

            // The more convenient way to get a type handle is to use SystemAPI,
            // which handles the caching and Update() for you.
            var rotationSpeedTypeHandle = SystemAPI.GetComponentTypeHandle<RotationSpeed>(true);

            var job = new RotationJob
            {
                TransformTypeHandle = transformTypeHandle,
                RotationSpeedTypeHandle = rotationSpeedTypeHandle,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            // Unlike an IJobEntity, an IJobChunk must be manually passed a query.
            // Furthermore, IJobChunk does not pass and assign the state.Dependency JobHandle implicitly.
            // (This pattern of passing and assigning state.Dependency ensures that the entity jobs scheduled
            // in different systems will depend upon each other as needed.)
            state.Dependency = job.ScheduleParallel(spinningCubesQuery, state.Dependency);
        }
    }

    [BurstCompile]
    struct RotationJob : IJobChunk
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
