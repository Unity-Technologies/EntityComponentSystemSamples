using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Miscellaneous.StateChangeStructural
{
    public partial struct SetStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Execute.StateChangeStructural>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hit = SystemAPI.GetSingleton<Hit>();

            if (hit.ChangedThisFrame)
            {
                var config = SystemAPI.GetSingleton<Config>();
                var sqRadius = config.Radius * config.Radius;
                var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

                new JobAdd
                {
                    SqRadius = sqRadius,
                    Hit = hit.Value,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                }.ScheduleParallel();

                new JobRemove
                {
                    SqRadius = sqRadius,
                    Hit = hit.Value,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                }.ScheduleParallel();
            }
        }
    }

    [WithAll(typeof(Cube))]
    [WithNone(typeof(Spinner))]
    [BurstCompile]
    partial struct JobAdd : IJobEntity
    {
        public float SqRadius;
        public float3 Hit;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform)
        {
            // If cube is inside the hit radius.
            if (math.distancesq(transform.Position, Hit) < SqRadius)
            {
                color.Value = (Vector4)Color.red;

                // The playback order of the commands in this job don't matter, so we'll just use 0 for the sortKey.
                ECB.AddComponent<Spinner>(0, entity);
            }
        }
    }

    [WithAll(typeof(Cube))]
    [WithAll(typeof(Spinner))]
    [BurstCompile]
    partial struct JobRemove : IJobEntity
    {
        public float SqRadius;
        public float3 Hit;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform)
        {
            // If cube is inside the hit radius.
            if (math.distancesq(transform.Position, Hit) < SqRadius)
            {
                color.Value = (Vector4)Color.white;

                // The playback order of the commands in this job don't matter, so we'll just use 0 for the sortKey.
                ECB.RemoveComponent<Spinner>(0, entity);
            }
        }
    }
}
