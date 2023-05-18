using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct PlayerSpawned : IComponentData { }

    public struct ConnectionOwner : IComponentData
    {
        public Entity Entity;
    }

    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class SpawnPlayerSystem : SystemBase
    {
        private EntityQuery m_NewPlayers;

        protected override void OnCreate()
        {
            RequireForUpdate(m_NewPlayers);
            // Must wait for the spawner entity scene to be streamed in, most likely instantaneous in
            // this sample but good to be sure
            RequireForUpdate<Spawner>();
            EntityQuery sceneQuery = GetEntityQuery(new EntityQueryDesc()
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<EnableSpawnPlayer>(), ComponentType.ReadOnly<EnableRemotePredictedPlayer>()
                }
            });
            RequireForUpdate(sceneQuery);
        }

        protected override void OnUpdate()
        {
            var prefab = SystemAPI.GetSingleton<Spawner>().Player;
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithName("SpawnPlayer").WithStoreEntityQueryInField(ref m_NewPlayers).WithNone<PlayerSpawned>().ForEach(
                (Entity connectionEntity, in NetworkStreamInGame req, in NetworkId networkId) =>
                {
                    Debug.Log($"Spawning player for connection {networkId.Value}");
                    var player = commandBuffer.Instantiate(prefab);

                    // The network ID owner must be set on the ghost owner component on the players
                    // this is used internally for example to set up the CommandTarget properly
                    commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });

                    // This is to support thin client players and you don't normally need to do this when the
                    // auto command target feature is used (enabled on the ghost authoring component on the prefab).
                    // See the ThinClients sample for more details.
                    commandBuffer.SetComponent(connectionEntity, new CommandTarget(){targetEntity = player});

                    // Mark that this connection has had a player spawned for it so we won't process it again
                    commandBuffer.AddComponent<PlayerSpawned>(connectionEntity);

                    // Add the player to the linked entity group on the connection so it is destroyed
                    // automatically on disconnect (destroyed with connection entity destruction)
                    commandBuffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup{Value = player});

                    commandBuffer.AddComponent(player, new ConnectionOwner { Entity = connectionEntity });
                }).Run();
            commandBuffer.Playback(EntityManager);
        }
    }
}
