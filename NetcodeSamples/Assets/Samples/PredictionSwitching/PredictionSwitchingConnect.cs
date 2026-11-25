using Unity.Collections;
using Unity.Netcode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.PredictionSwitching
{
    // Tag added to new connections on the server so they'll only be processed once (set up new client)
    public struct ConnectionInitialized : IComponentData { }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PredictionSwitchingConnectServerSystem : SystemBase
    {
        ComponentLookup<NetworkStreamIsReconnected> m_ReconnectedLookup;

        protected override void OnCreate()
        {
            RequireForUpdate<PredictionSwitchingSettings>();
            m_ReconnectedLookup = GetComponentLookup<NetworkStreamIsReconnected>(true);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var settings = SystemAPI.GetSingleton<PredictionSwitchingSettings>();
            var worldName = World.Unmanaged.Name;
            m_ReconnectedLookup.Update(this);
            foreach (var (target, netId, connection) in
                     SystemAPI.Query<RefRW<CommandTarget>, RefRO<NetworkId>>().WithNone<ConnectionInitialized>().WithEntityAccess())
            {
                commandBuffer.AddComponent<ConnectionInitialized>(connection);

                if (m_ReconnectedLookup.HasComponent(connection))
                {
                    Debug.Log($"'{worldName}' connection '{netId.ValueRO.Value}' has reconnected!");
                    continue;
                }

                target.ValueRW.targetEntity = EntityManager.Instantiate(settings.Player);
                EntityManager.SetComponentData(target.ValueRW.targetEntity, new GhostOwner { NetworkId = netId.ValueRO.Value });

                // Spawn at the edge of the field, in a line.
                var isEven = (netId.ValueRO.Value & 1) == 0;
                const float halfCharacterSpawnSeparation = 1.3f;
                const float spawnStaggeredOffset = 0.65f;
                var staggeredXPos = netId.ValueRO.Value * math.@select(halfCharacterSpawnSeparation, -halfCharacterSpawnSeparation, isEven) + math.@select(-spawnStaggeredOffset, spawnStaggeredOffset, isEven);
                const float halfCapsuleHeight = 1f;
                const float nearMapWall = -18.5f;

                var transform = EntityManager.GetComponentData<LocalTransform>(target.ValueRW.targetEntity);
                EntityManager.SetComponentData(target.ValueRW.targetEntity,
                    transform.WithPosition(new float3(staggeredXPos, halfCapsuleHeight, nearMapWall)));

                // Add the player to the linked entity group so it is destroyed automatically on disconnect
                EntityManager.GetBuffer<LinkedEntityGroup>(connection).Add(new LinkedEntityGroup { Value = target.ValueRW.targetEntity });
            }
            commandBuffer.Playback(EntityManager);
        }
    }
}
