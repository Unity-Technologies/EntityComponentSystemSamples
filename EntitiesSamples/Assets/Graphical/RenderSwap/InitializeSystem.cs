using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Graphical.RenderSwap
{
    public partial struct InitializeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<ExecuteRenderSwap>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var config = SystemAPI.GetSingleton<Config>();
            var entities = CollectionHelper.CreateNativeArray<Entity>(config.Size * config.Size,
                state.WorldUnmanaged.UpdateAllocator.ToAllocator);
            ecb.Instantiate(config.StateOff, entities);

            var localTransform = SystemAPI.GetComponent<LocalTransform>(config.StateOff);
            var offset = (1 - config.Size) / 2f;
            for (int x = 0; x < config.Size; x++)
            {
                for (int z = 0; z < config.Size; z++)
                {
                    localTransform.Position = new float3(x + offset, 0, z + offset);
                    ecb.SetComponent(entities[x + z * config.Size], localTransform);
                }
            }
        }
    }
}
