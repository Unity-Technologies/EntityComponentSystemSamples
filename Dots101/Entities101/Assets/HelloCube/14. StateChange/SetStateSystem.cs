using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Unity.Profiling.LowLevel.Unsafe;

namespace HelloCube.StateChange
{
    public partial struct SetStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<ExecuteStateChange>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var hit = SystemAPI.GetSingleton<Hit>();

            if (!hit.HitChanged)
            {
#if UNITY_EDITOR
                SystemAPI.GetSingletonRW<StateChangeProfilerModule.FrameData>().ValueRW.SetStatePerf = 0;
#endif
                return;
            }

            var radiusSq = config.Radius * config.Radius;
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

            state.Dependency.Complete();
            var before = ProfilerUnsafeUtility.Timestamp;

            if (config.Mode == Mode.VALUE)
            {
                new SetValueJob
                {
                    RadiusSq = radiusSq,
                    Hit = hit.Value
                }.ScheduleParallel();
            }
            else if (config.Mode == Mode.STRUCTURAL_CHANGE)
            {
                new AddSpinJob
                {
                    RadiusSq = radiusSq,
                    Hit = hit.Value,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                }.ScheduleParallel();

                new RemoveSpinJob
                {
                    RadiusSq = radiusSq,
                    Hit = hit.Value,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                }.ScheduleParallel();
            }
            else if (config.Mode == Mode.ENABLEABLE_COMPONENT)
            {
                new EnableSpinJob
                {
                    RadiusSq = radiusSq,
                    Hit = hit.Value,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                }.ScheduleParallel();

                new DisableSpinJob
                {
                    RadiusSq = radiusSq,
                    Hit = hit.Value,
                }.ScheduleParallel();
            }

            state.Dependency.Complete();
            var after = ProfilerUnsafeUtility.Timestamp;

#if UNITY_EDITOR
            // profiling
            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            var elapsed = (after - before) * conversionRatio.Numerator / conversionRatio.Denominator;
            SystemAPI.GetSingletonRW<StateChangeProfilerModule.FrameData>().ValueRW.SetStatePerf = elapsed;
#endif
        }
    }

    [BurstCompile]
    partial struct SetValueJob : IJobEntity
    {
        public float RadiusSq;
        public float3 Hit;

        void Execute(ref URPMaterialPropertyBaseColor color, ref Spin spin, in LocalTransform transform)
        {
            if (math.distancesq(transform.Position, Hit) <= RadiusSq)
            {
                color.Value = (Vector4)Color.red;
                spin.IsSpinning = true;
            }
            else
            {
                color.Value = (Vector4)Color.white;
                spin.IsSpinning = false;
            }
        }
    }

    [WithNone(typeof(Spin))]
    [BurstCompile]
    partial struct AddSpinJob : IJobEntity
    {
        public float RadiusSq;
        public float3 Hit;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform,
            [ChunkIndexInQuery] int chunkIndex)
        {
            // If cube is inside the hit radius.
            if (math.distancesq(transform.Position, Hit) <= RadiusSq)
            {
                color.Value = (Vector4)Color.red;
                ECB.AddComponent<Spin>(chunkIndex, entity);
            }
        }
    }

    [WithAll(typeof(Spin))]
    [BurstCompile]
    partial struct RemoveSpinJob : IJobEntity
    {
        public float RadiusSq;
        public float3 Hit;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform,
            [ChunkIndexInQuery] int chunkIndex)
        {
            // If cube is NOT inside the hit radius.
            if (math.distancesq(transform.Position, Hit) > RadiusSq)
            {
                color.Value = (Vector4)Color.white;
                ECB.RemoveComponent<Spin>(chunkIndex, entity);
            }
        }
    }

    [WithNone(typeof(Spin))]
    [BurstCompile]
    public partial struct EnableSpinJob : IJobEntity
    {
        public float RadiusSq;
        public float3 Hit;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform,
            [ChunkIndexInQuery] int chunkIndex)
        {
            // If cube is inside the hit radius.
            if (math.distancesq(transform.Position, Hit) <= RadiusSq)
            {
                color.Value = (Vector4)Color.red;
                ECB.SetComponentEnabled<Spin>(chunkIndex, entity, true);
            }
        }
    }

    [BurstCompile]
    public partial struct DisableSpinJob : IJobEntity
    {
        public float RadiusSq;
        public float3 Hit;

        void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform,
            EnabledRefRW<Spin> spinnerEnabled)
        {
            // If cube is NOT inside the hit radius.
            if (math.distancesq(transform.Position, Hit) > RadiusSq)
            {
                color.Value = (Vector4)Color.white;
                spinnerEnabled.ValueRW = false;
            }
        }
    }
}
