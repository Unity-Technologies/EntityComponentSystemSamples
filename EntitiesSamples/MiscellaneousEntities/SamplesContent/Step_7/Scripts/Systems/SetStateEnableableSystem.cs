using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace StateChange
{
    [BurstCompile]
    public partial struct SetStateEnableableSystem : ISystem
    {
        [BurstCompile]
        [WithAll(typeof(State))]
        [WithNone(typeof(StateEnabled))]
        partial struct JobAdd : IJobEntity
        {
            public float SqRadius;
            public float3 Hit;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<StateEnabled> StateEnabledFromEntity;

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

        [BurstCompile]
        [WithAll(typeof(State))]
        [WithAll(typeof(StateEnabled))]
        partial struct JobRemove : IJobEntity
        {
            public float SqRadius;
            public float3 Hit;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<StateEnabled> StateEnabledFromEntity;

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

        public struct EnableSingleton : IComponentData { }

        public struct State : IComponentData { }

        public struct StateEnabled : IComponentData, IEnableableComponent { }

        ComponentLookup<StateEnabled> m_StateEnabledFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableSingleton>();
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();

            m_StateEnabledFromEntity = state.GetComponentLookup<StateEnabled>();
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
                m_StateEnabledFromEntity.Update(ref state);
                var config = SystemAPI.GetSingleton<Config>();
                var sqRadius = config.Radius * config.Radius;
                var hit = SystemAPI.GetSingleton<Hit>().Value;

                var jobAdd = new JobAdd
                {
                    SqRadius = sqRadius,
                    Hit = hit,
                    StateEnabledFromEntity = m_StateEnabledFromEntity
                };

                var jobRemove = new JobRemove
                {
                    SqRadius = sqRadius,
                    Hit = hit,
                    StateEnabledFromEntity = m_StateEnabledFromEntity
                };

                jobAdd.ScheduleParallel();
                jobRemove.ScheduleParallel();
                state.Dependency.Complete();
            }
            var after = ProfilerUnsafeUtility.Timestamp;

            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            var elapsed = (after - before) * conversionRatio.Numerator / conversionRatio.Denominator;
            SystemAPI.GetSingletonRW<StateChangeProfilerModule.FrameData>().ValueRW.SetStatePerf = elapsed;
        }
    }
}
