using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace StateMachine
{
    [BurstCompile]
    public partial struct SetStateSystem : ISystem
    {
        ComponentLookup<Spinner> spinnerLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            spinnerLookup = SystemAPI.GetComponentLookup<Spinner>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            spinnerLookup.Update(ref state);
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var hit = SystemAPI.GetSingleton<Hit>();
            if (hit.ChangedThisFrame)
            {
                var config = SystemAPI.GetSingleton<Config>();

                var enableSpinnerJob = new EnableSpinnerJob
                {
                    SqRadius = config.Radius * config.Radius,
                    Hit = hit.Value,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                };
                enableSpinnerJob.ScheduleParallel();
                
                var disableSpinnerJob = new DisableSpinnerJob
                {
                    SqRadius = config.Radius * config.Radius,
                    Hit = hit.Value,
                };
                disableSpinnerJob.ScheduleParallel();
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(Cube))]
    [WithNone(typeof(Spinner))]
    public partial struct EnableSpinnerJob : IJobEntity
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
                ECB.SetComponentEnabled<Spinner>(0, entity, true);
            }
        }
    }
    
    [BurstCompile]
    [WithAll(typeof(Cube))]
    public partial struct DisableSpinnerJob : IJobEntity
    {
        public float SqRadius;
        public float3 Hit;

        void Execute(Entity entity, ref URPMaterialPropertyBaseColor color, in LocalTransform transform, EnabledRefRW<Spinner> enabledRefRw)
        {
            // If cube is inside the hit radius.
            if (math.distancesq(transform.Position, Hit) < SqRadius)
            {
                color.Value = (Vector4)Color.white;
                enabledRefRw.ValueRW = false;
            }
        }
    }
}