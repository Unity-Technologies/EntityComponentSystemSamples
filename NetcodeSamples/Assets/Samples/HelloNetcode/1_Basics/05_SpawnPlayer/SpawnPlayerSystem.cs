using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Flag component, denoting whether or not a Player Character Controller (CC) has been spawned
    /// for a given connection.
    /// </summary>
    public struct PlayerSpawned : IComponentData { }

    /// <summary>
    ///     Convenience: This allows us to trivially fetch the connection entity associated with
    ///     this player character controller entity.
    /// </summary>
    public struct ConnectionOwner : IComponentData
    {
        public Entity Entity;
    }

    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct SpawnPlayerSystem : ISystem
    {
        private EntityQuery m_NewPlayersQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Must wait for the spawner entity scene to be streamed in,
            // which is most likely instantaneous in this sample (but good to be sure).
            state.RequireForUpdate<Spawner>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<EnableSpawnPlayer, EnableRemotePredictedPlayer>().Build());
            m_NewPlayersQuery = SystemAPI.QueryBuilder().WithAll<NetworkId>().WithNone<PlayerSpawned>().Build();
            state.RequireForUpdate(m_NewPlayersQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var prefab = SystemAPI.GetSingleton<Spawner>().Player;
            state.EntityManager.GetName(prefab, out var prefabName);
            if (prefabName.IsEmpty) prefabName = prefab.ToFixedString();

            // Iterate through all connections events raised by netcode,
            // and if they're new joiners, spawn a player character controller for them:
            var connectionEntities = m_NewPlayersQuery.ToEntityArray(Allocator.Temp);
            var networkIds = m_NewPlayersQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);
            for (var i = 0; i < connectionEntities.Length; i++)
            {
                var networkId = networkIds[i];
                var connectionEntity = connectionEntities[i];
                var player = state.EntityManager.Instantiate(prefab);
                Debug.Log($"[SpawnPlayerSystem][{state.WorldUnmanaged.Name}] Spawning player CC '{player.ToFixedString()}' (from prefab '{prefabName}') for {networkId.ToFixedString()}.");

                // Offset the spawn position so that ghosts don't spawn on top of each other.
                // In a real game, you'd have set spawn locations/zones.
                var localTransform = state.EntityManager.GetComponentData<LocalTransform>(prefab);
                localTransform.Position.x += networkId.Value * 2;
                state.EntityManager.SetComponentData(player, localTransform);

                // The network ID owner must be set on the spawned ghost.
                // Doing so gives said client the authority to raise inputs for (i.e. to control) this ghost.
                state.EntityManager.SetComponentData(player, new GhostOwner {NetworkId = networkId.Value});

                // This is to support thin client players.
                // You don't normally need to do this, as it's typically easier to simply enable the AutoCommandTarget
                // (via the GhostAuthoringComponent).
                // See the ThinClients sample for more details.
                state.EntityManager.SetComponentData(connectionEntity, new CommandTarget {targetEntity = player});

                // Add the player to the linked entity group on the connection, so it is destroyed
                // automatically on disconnect (i.e. it's destroyed along with the connection entity,
                // when the connection entity is destroyed).
                state.EntityManager.GetBuffer<LinkedEntityGroup>(connectionEntity).Add(new LinkedEntityGroup {Value = player});

                // This is a convenience: It allows us to trivially fetch the connection entity associated with
                // this player character controller entity.
                state.EntityManager.AddComponentData(player, new ConnectionOwner {Entity = connectionEntity});

                // Mark that this connection has had a player spawned for it, so we won't process it again:
                state.EntityManager.AddComponent<PlayerSpawned>(connectionEntity);
            }
        }
    }
}
