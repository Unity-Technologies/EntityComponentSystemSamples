using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>Simple IMGUI example rendering of a player scoreboard with connect/disconnect notifications.</summary>
    public class RenderPlayerListMb : MonoBehaviour
    {
        public AnimationCurve notificationOpacityCurve = AnimationCurve.Linear(0, 1, 1, 0);
        public GUISkin skin;
        public bool debugShowThinClients;

        GUILayoutOption m_LabelWidth = GUILayout.Width(300-40);
        GUILayoutOption m_NetIdWidth = GUILayout.Width(35);
        GUILayoutOption m_ScoreboardWidth = GUILayout.Width(300);

        Dictionary<World, WorldCache> m_WorldCaches = new Dictionary<World, WorldCache>(1);
        ulong m_LastSequenceNumber;

        class WorldCache
        {
            public EntityQuery NetworkIdQuery;
            public EntityQuery DesiredUsernameQuery;
            public EntityQuery PlayerListNotificationBufferQuery;
            public EntityQuery PlayerListBufferEntryQuery;
            public string DesiredUsername;
            public bool IsModifying;
        }

        void Update()
        {
            if (World.NextSequenceNumber != m_LastSequenceNumber)
            {
                foreach (var world in World.All)
                {
                    if(world.SequenceNumber >= m_LastSequenceNumber && world.IsClient())
                    {
                        if (!world.IsThinClient() || (debugShowThinClients && world.IsThinClient()))
                        {
                            if (!m_WorldCaches.ContainsKey(world))
                            {
                                m_WorldCaches[world] = new WorldCache
                                {
                                    NetworkIdQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>()),
                                    DesiredUsernameQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<DesiredUsername>()),
                                    PlayerListBufferEntryQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerListBufferEntry>()),
                                    PlayerListNotificationBufferQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerListNotificationBuffer>())
                                };
                            }
                        }
                    }
                }
                m_LastSequenceNumber = World.NextSequenceNumber;

                CleanUpWorldCache();
            }
        }

        void CleanUpWorldCache()
        {
            bool repeat;
            do
            {
                repeat = false;
                foreach (var kvp in m_WorldCaches)
                {
                    if (!kvp.Key.IsCreated)
                    {
                        m_WorldCaches.Remove(kvp.Key);
                        repeat = true;
                        break;
                    }
                }
            } while (repeat);
        }

        void OnGUI()
        {
            GUI.skin = skin;

            GUILayout.BeginHorizontal();
            {
                // Clients:
                foreach (var worldKvp in m_WorldCaches)
                {
                    GUILayout.BeginVertical();
                    DrawPlayerListForClientWorld(worldKvp);
                    GUILayout.EndVertical();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void DrawPlayerListForClientWorld(KeyValuePair<World, WorldCache> kvp)
        {
            var (world, cache) = kvp;
            if (!world.IsCreated) { return; }
            if (cache.DesiredUsernameQuery.IsEmptyIgnoreFilter) { return; }

            GUI.color = Color.yellow;
            if(debugShowThinClients) GUILayout.Box(world.Name);
            GUILayout.Box("PLAYER LIST", m_ScoreboardWidth);

            if (!cache.IsModifying)
                cache.DesiredUsername = cache.DesiredUsernameQuery.GetSingleton<DesiredUsername>().Value.ToString();

            var newNameString = GUILayout.TextField(cache.DesiredUsername, m_ScoreboardWidth);
            if (newNameString != cache.DesiredUsername)
            {
                cache.DesiredUsername = newNameString;
                cache.IsModifying = true;
            }

            if (cache.IsModifying)
            {
                var hasNoControlFocus = GUIUtility.keyboardControl == 0;
                var returnKeyPressed = Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter;
                if (hasNoControlFocus || returnKeyPressed)
                {
                    ref var desiredUsernameStoreRw = ref cache.DesiredUsernameQuery.GetSingletonRW<DesiredUsername>().ValueRW;
                    FixedStringMethods.CopyFromTruncated(ref desiredUsernameStoreRw.Value, newNameString);
                }
            }

            GUI.color = Color.grey;
            var networkId = cache.NetworkIdQuery.IsEmptyIgnoreFilter ? -1 : cache.NetworkIdQuery.GetSingleton<NetworkId>().Value;
            if (networkId <= 0)
            {
                GUILayout.Box("Disconnected.");
                return;
            }

            DrawPlayerList(cache);

            DrawPlayerNotifications(cache);
        }

        void DrawPlayerNotifications(WorldCache worldCache)
        {
            if (worldCache.PlayerListNotificationBufferQuery.IsEmptyIgnoreFilter) return;

            GUILayout.EndVertical();
            GUILayout.BeginVertical();

            var notifications = worldCache.PlayerListNotificationBufferQuery.GetSingletonBuffer<PlayerListNotificationBuffer>();
            GUI.color = Color.yellow;
            GUILayout.Box("NOTIFICATIONS");
            GUI.color = Color.white;

            var now = Stopwatch.GetTimestamp();
            foreach (var entry in notifications)
                DrawNotification(entry, now);
        }

        void DrawPlayerList(WorldCache cache)
        {
            if (cache.PlayerListBufferEntryQuery.IsEmptyIgnoreFilter) return;

            var players = cache.PlayerListBufferEntryQuery.GetSingletonBuffer<PlayerListBufferEntry>();

            var drawnPlayers = 0;
            GUI.color = Color.white;
            for (var i = 0; i < players.Length; i++)
            {
                var entry = players[i];
                if (!entry.IsCreated || !entry.State.IsConnected) continue;

                drawnPlayers++;
                GUILayout.BeginHorizontal();
                {
                    GUI.color = entry.State.IsConnected ? entry.State.Username.Value.IsEmpty ? Color.grey : Color.white : Color.red;

                    GUILayout.Box(entry.State.NetworkId.ToString(), m_NetIdWidth);

                    GUILayout.Label(entry.State.Username.Value.Value, m_LabelWidth);
                }
                GUILayout.EndHorizontal();
            }

            if (drawnPlayers == 0) GUILayout.Box("No players.");
        }

        void DrawNotification(PlayerListNotificationBuffer entry, long now)
        {
            string notificationText;
            var username = entry.Event.Username.Value;
            Color color;
            if (entry.Event.IsConnected)
            {
                notificationText = $"{username} connected!";
                color = Color.green;
            }
            else
            {
                switch (entry.Event.Reason)
                {
                    case NetworkStreamDisconnectReason.Timeout:
                        notificationText = $"{username} timed out!";
                        color = Color.red;
                        break;
                    case NetworkStreamDisconnectReason.MaxConnectionAttempts:
                        notificationText = $"{username} exceeded max connection attempts!";
                        color = Color.red;
                        break;
                    case NetworkStreamDisconnectReason.ClosedByRemote:
                        notificationText = $"{username} quit!";
                        color = new Color(1f, 0.39f, 0.43f);
                        break;
                    case NetworkStreamDisconnectReason.ConnectionClose:
                        notificationText = $"{username} was disconnected by server!";
                        color = new Color(1f, 0.39f, 0.43f);
                        break;
                    case NetworkStreamDisconnectReason.InvalidRpc:
                        notificationText = $"{username} had invalid RPC!";
                        color = new Color(0.39f, 0.01f, 0.63f);
                        break;
                    case NetworkStreamDisconnectReason.BadProtocolVersion:
                        notificationText = $"{username} had invalid protocol version!";
                        color = new Color(0.39f, 0.01f, 0.63f);
                        break;
                    default:
                        notificationText = $"{username} disconnected with error {(int) entry.Event.Reason}!";
                        color = new Color(0.39f, 0.01f, 0.63f);
                        break;
                }
            }

            var targetAlpha = notificationOpacityCurve.Evaluate(entry.DurationLeft);
            GUI.color = Color.LerpUnclamped(Color.clear, color, targetAlpha);
            GUILayout.Box(notificationText, m_ScoreboardWidth);
        }
    }
}
