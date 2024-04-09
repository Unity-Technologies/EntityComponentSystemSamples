using Unity.Burst;
using Unity.Entities;

namespace Graphical.RenderSwap
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct ConfigBakingSystem : ISystem
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
            var config = SystemAPI.GetSingleton<Config>();
            state.EntityManager.AddComponent<SpinTile>(config.StateOn);
            state.EntityManager.AddComponent<SpinTile>(config.StateOff);
        }
    }

    struct SpinTile : IComponentData
    {
    }
}
