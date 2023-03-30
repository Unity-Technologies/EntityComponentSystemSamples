using Miscellaneous.Execute;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Miscellaneous.StateChange
{
    [BurstCompile]
    public partial struct SetStateStructuralChangeSystem : ISystem
    {
        public struct EnableSingleton : IComponentData
        {
        }

        public struct State : IComponentData
        {
        }

        public struct StateEnabled : IComponentData
        {
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<EnableSingleton>();
            state.RequireForUpdate<StateChangeProfiling>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var before = ProfilerUnsafeUtility.Timestamp;
            {
                var config = SystemAPI.GetSingleton<Config>();
                var sqRadius = config.Radius * config.Radius;
                var hit = SystemAPI.GetSingleton<Hit>().Value;

                var ecb = new EntityCommandBuffer(state.WorldUnmanaged.UpdateAllocator.ToAllocator);

                var jobAdd = new JobAdd
                {
                    SqRadius = sqRadius,
                    Hit = hit,
                    ECB = ecb.AsParallelWriter()
                };

                var jobRemove = new JobRemove
                {
                    SqRadius = sqRadius,
                    Hit = hit,
                    ECB = ecb.AsParallelWriter()
                };

                jobAdd.ScheduleParallel();
                jobRemove.ScheduleParallel();
                state.Dependency.Complete();

                ecb.Playback(state.EntityManager);
            }
            var after = ProfilerUnsafeUtility.Timestamp;

            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            var elapsed = (after - before) * conversionRatio.Numerator / conversionRatio.Denominator;
            SystemAPI.GetSingletonRW<StateChangeProfilerModule.FrameData>().ValueRW.SetStatePerf = elapsed;
        }

        [WithAll(typeof(State))]
        [WithNone(typeof(StateEnabled))]
        [BurstCompile]
        partial struct JobAdd : IJobEntity
        {
            public float SqRadius;
            public float3 Hit;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform)
            {
                bool inside = math.distancesq(transform.Position, Hit) < SqRadius;
                if (inside)
                {
                    color.Value = (Vector4)Color.red;
                    ECB.AddComponent<StateEnabled>(0, entity);
                }
            }
        }

        [WithAll(typeof(State))]
        [WithAll(typeof(StateEnabled))]
        [BurstCompile]
        partial struct JobRemove : IJobEntity
        {
            public float SqRadius;
            public float3 Hit;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform)
            {
                bool inside = math.distancesq(transform.Position, Hit) < SqRadius;
                if (!inside)
                {
                    color.Value = (Vector4)Color.white;
                    ECB.RemoveComponent<StateEnabled>(0, entity);
                }
            }
        }
    }
}
