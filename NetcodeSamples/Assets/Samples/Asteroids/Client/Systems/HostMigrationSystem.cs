using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Asteroids.Client
{
    /// <summary>
    /// Custom host migration logic for clients in the Asteroids sample. Place the client immediately in game again
    /// after a host migration.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation|WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct HostMigrationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamDriver>();
            state.RequireForUpdate<LevelComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var evt in SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick)
            {
                var reconnected = SystemAPI.GetComponentLookup<NetworkStreamIsReconnected>();
                if (evt.State == ConnectionState.State.Connected && reconnected.HasComponent(evt.ConnectionEntity))
                {
                    state.EntityManager.AddComponent<NetworkStreamInGame>(evt.ConnectionEntity);
                    // Remove the reconnection tag as we're done with it
                    state.EntityManager.RemoveComponent<NetworkStreamIsReconnected>(evt.ConnectionEntity);
                }
            }
        }
    }
}
