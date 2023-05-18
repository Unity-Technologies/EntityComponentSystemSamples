using Miscellaneous.Execute;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Miscellaneous.StateChange
{
    public partial struct SetStateEnableableSystem : ISystem
    {
        public struct EnableSingleton : IComponentData
        {
        }

        public struct State : IComponentData
        {
        }

        public struct StateEnabled : IComponentData, IEnableableComponent
        {
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableSingleton>();
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<StateChangeProfiling>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var before = ProfilerUnsafeUtility.Timestamp;
            {
                var stateEnabledLookup = SystemAPI.GetComponentLookup<StateEnabled>();
                var config = SystemAPI.GetSingleton<Config>();
                var sqRadius = config.Radius * config.Radius;
                var hit = SystemAPI.GetSingleton<Hit>().Value;

                new JobAdd
                {
                    SqRadius = sqRadius,
                    Hit = hit,
                    StateEnabledFromEntity = stateEnabledLookup
                }.ScheduleParallel();

                new JobRemove
                {
                    SqRadius = sqRadius,
                    Hit = hit,
                    StateEnabledFromEntity = stateEnabledLookup
                }.ScheduleParallel();

                state.Dependency.Complete();
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

            [NativeDisableParallelForRestriction] public ComponentLookup<StateEnabled> StateEnabledFromEntity;

            void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform)
            {
                bool inside = math.distancesq(transform.Position, Hit) < SqRadius;
                if (inside)
                {
                    color.Value = (Vector4)Color.red;
                    StateEnabledFromEntity.SetComponentEnabled(entity, true);
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

            [NativeDisableParallelForRestriction] public ComponentLookup<StateEnabled> StateEnabledFromEntity;

            void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform)
            {
                bool inside = math.distancesq(transform.Position, Hit) < SqRadius;
                if (!inside)
                {
                    color.Value = (Vector4)Color.white;
                    StateEnabledFromEntity.SetComponentEnabled(entity, false);
                }
            }
        }
    }
}
