using Unity.Entities;
using Unity.NetCode;
using Unity.Rendering;

namespace Asteroids.Client
{
    /// <summary>
    /// Assign the current color value to the render material, this will only trigger when the
    /// value changes.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerColorSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (color, material) in SystemAPI.Query<RefRO<PlayerColor>, RefRW<URPMaterialPropertyBaseColor>>().WithChangeFilter<MaterialMeshInfo, PlayerColor>())
            {
                material.ValueRW.Value = NetworkIdDebugColorUtility.Get(color.ValueRO.Value);
            }
        }
    }
}
