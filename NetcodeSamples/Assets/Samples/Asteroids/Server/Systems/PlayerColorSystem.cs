using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Asteroids.Server
{
    /// <summary>
    /// When a new connection connects, assign a color to it which will be
    /// used by the player ship for the duration of the session. Connections
    /// which are reconnecting after a migration will already have the right
    /// color component assigned automatically.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    public partial struct PlayerColorSystem : ISystem
    {
        ComponentLookup<IsReconnected> m_IsReconnectedLookup;
        public void OnCreate(ref SystemState state)
        {
            m_IsReconnectedLookup = state.GetComponentLookup<IsReconnected>();
            state.RequireForUpdate<AsteroidsSpawner>();
            state.RequireForUpdate<NetworkStreamDriver>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<HostMigrationInProgress>())
                return;

            if (!SystemAPI.HasSingleton<PlayerColorNext>())
            {
                var spawner = SystemAPI.GetSingleton<AsteroidsSpawner>();
                state.EntityManager.Instantiate(spawner.HostConfig);
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<PlayerColor>().WithEntityAccess())
            {
                SystemAPI.TryGetSingletonRW<PlayerColorNext>(out var nextColor);
                ecb.AddComponent(entity, new PlayerColor{Value = nextColor.ValueRW.Value++});
                m_IsReconnectedLookup.Update(ref state);
                if (m_IsReconnectedLookup.HasComponent(entity))
                    ecb.RemoveComponent<IsReconnected>(entity);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
