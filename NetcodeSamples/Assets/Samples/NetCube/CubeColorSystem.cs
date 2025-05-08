using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Rendering;

/// <summary>
/// The next color available to the next player/connection which joins.
/// The color is then assigned to that player for the duration of their session.
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct CubeColorNext : IComponentData
{
    [GhostField] public int Value;
}

/// <summary>
/// When a new connection connects, assign a color to it which will be
/// used by the player ship for the duration of the session. Connections
/// which are reconnecting after a migration will already have the right
/// color component assigned automatically.
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
public partial struct ServerCubeColorSystem : ISystem
{
    ComponentLookup<NetworkStreamIsReconnected> isReconnectedLookup;
    public void OnCreate(ref SystemState state)
    {
        isReconnectedLookup = state.GetComponentLookup<NetworkStreamIsReconnected>();
        state.RequireForUpdate<CubeSpawner>();
        state.RequireForUpdate<NetworkStreamDriver>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<HostMigrationInProgress>())
            return;

        if (!SystemAPI.HasSingleton<CubeConfig>())
        {
            var spawner = SystemAPI.GetSingleton<CubeSpawner>();
            state.EntityManager.Instantiate(spawner.Config);
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (_, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<CubeColor>().WithEntityAccess())
        {
            if (SystemAPI.TryGetSingletonRW<CubeConfig>(out var nextColor))
            {
                ecb.AddComponent(entity, new CubeColor{Value = nextColor.ValueRW.NextColorValue++});
                isReconnectedLookup.Update(ref state);
                if (isReconnectedLookup.HasComponent(entity))
                    ecb.RemoveComponent<NetworkStreamIsReconnected>(entity);
            }
        }
        ecb.Playback(state.EntityManager);
    }
}

/// <summary>
/// Assign the current color value to the render material, this will only trigger when the
/// value changes.
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientCubeColorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (color, material) in SystemAPI.Query<RefRO<CubeColor>, RefRW<URPMaterialPropertyBaseColor>>().WithChangeFilter<MaterialMeshInfo, CubeColor>())
        {
            material.ValueRW.Value = NetworkIdDebugColorUtility.Get(color.ValueRO.Value);
        }
    }
}
