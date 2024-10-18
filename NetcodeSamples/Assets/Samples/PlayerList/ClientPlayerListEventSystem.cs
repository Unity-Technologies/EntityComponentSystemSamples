using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>
    ///     Receives <see cref="Unity.NetCode.Samples.PlayerList.PlayerListEntry" /> RPC's, notifying this client of the UPDATE NOTIFICATIONS to other clients.
    ///     Raises <see cref="PlayerListNotificationBuffer"/> entries in a singleton buffer.
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ClientPlayerListEventSystem : ISystem, ISystemStartStop
    {
        EntityQuery m_PlayerListEntryChangedRpc;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnablePlayerListsFeature>();
            state.RequireForUpdate<NetworkId>();

            var bufferSingleton = state.EntityManager.CreateEntity();
            state.EntityManager.AddBuffer<PlayerListNotificationBuffer>(bufferSingleton);
            state.EntityManager.SetName(bufferSingleton, (FixedString64Bytes)"PlayerListNotificationBuffer");

            m_PlayerListEntryChangedRpc = state.GetEntityQuery(ComponentType.ReadOnly<PlayerListEntry.ChangedRpc>());
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            ref var playerListsFeature = ref SystemAPI.GetSingletonRW<EnablePlayerListsFeature>().ValueRW;
            if (playerListsFeature.EventListEntryDurationSeconds == default) playerListsFeature.EventListEntryDurationSeconds = 5f;
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            SystemAPI.GetSingletonBuffer<PlayerListNotificationBuffer>(false).Clear();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var eventList = SystemAPI.GetSingletonBuffer<PlayerListNotificationBuffer>(false);
            var playerListsFeature = SystemAPI.GetSingleton<EnablePlayerListsFeature>();
            if (playerListsFeature.EventListEntryDurationSeconds == -1)
            {
                eventList.Clear();
                return;
            }

            // Clear stale events:
            for (var i = 0; i < eventList.Length; i++)
            {
                var element = eventList[i];
                element.DurationLeft -= SystemAPI.Time.DeltaTime;
                if (element.DurationLeft <= 0)
                {
                    eventList.RemoveAt(i);
                    i--;
                }
                else eventList[i] = element;
            }

            // Add new:
            if (!m_PlayerListEntryChangedRpc.IsEmptyIgnoreFilter)
            {
                var netDebug = SystemAPI.GetSingleton<NetDebug>();
                foreach (var rpc in m_PlayerListEntryChangedRpc.ToComponentDataArray<PlayerListEntry.ChangedRpc>(Allocator.Temp))
                {
                    Assertions.Assert.AreNotEqual(default, playerListsFeature.EventListEntryDurationSeconds);
                    eventList.Add(new PlayerListNotificationBuffer
                        {
                            Event = rpc,
                            DurationLeft = (float) playerListsFeature.EventListEntryDurationSeconds,
                        });
                }
            }
        }
    }
}
