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

        const int k_ScoreboardWidth = 220;
        const int k_NetIdWidth = 33;
        static readonly GUILayoutOption s_UsernameWidth = GUILayout.Width(k_ScoreboardWidth-(k_NetIdWidth+5));
        static readonly GUILayoutOption s_NetIdWidth = GUILayout.Width(k_NetIdWidth);
        static readonly GUILayoutOption s_ScoreboardWidth = GUILayout.Width(k_ScoreboardWidth);
        static readonly GUILayoutOption s_NotificationWidth = GUILayout.Width(k_ScoreboardWidth);

        Dictionary<World, WorldCache> m_WorldCaches = new Dictionary<World, WorldCache>(1);
        ulong m_LastSequenceNumber;
        private int m_ScreenWidth;
        private int m_ScreenHeight;

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
            m_ScreenWidth = Screen.width;
            m_ScreenHeight = Screen.height;
            GUI.skin = skin;

            GUILayout.BeginHorizontal();
            {
                // Clients:
                foreach (var worldKvp in m_WorldCaches)
                {
                    GUILayout.BeginVertical();
                    DrawPlayerListForClientWorld(worldKvp);
                    GUILayout.EndVertical();

                    if (IsGuiFullyOffScreen()) break;
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
            GUILayout.Box("PLAYER LIST", s_ScoreboardWidth);

            if (!cache.IsModifying)
                cache.DesiredUsername = cache.DesiredUsernameQuery.GetSingleton<DesiredUsername>().Value.ToString();

            var newNameString = GUILayout.TextField(cache.DesiredUsername, s_ScoreboardWidth);
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

            GUI.color = Color.yellow;
            GUILayout.Box("NOTIFICATIONS", s_NotificationWidth);
            var notifications = worldCache.PlayerListNotificationBufferQuery.GetSingletonBuffer<PlayerListNotificationBuffer>();

            GUI.color = Color.white;
            foreach (var entry in notifications)
            {
                DrawNotification(entry);
                if (IsGuiFullyOffScreen()) break;
            }
        }

        void DrawPlayerList(WorldCache cache)
        {
            if (cache.PlayerListBufferEntryQuery.IsEmptyIgnoreFilter)
            {
                GUI.color = Color.red;
                GUILayout.Box($"No PlayerListBufferEntry!", s_ScoreboardWidth);
                return;
            }

            var playerEntries = cache.PlayerListBufferEntryQuery.GetSingletonBuffer<PlayerListBufferEntry>();
            GUILayout.Box($"{PlayerListBufferEntry.CountNumConnectedPlayers(playerEntries)} Players in Game", s_ScoreboardWidth);

            for (var i = 0; i < playerEntries.Length; i++)
            {
                var entry = playerEntries[i];
                if (!entry.IsCreated || !entry.State.IsConnected) continue;
                GUILayout.BeginHorizontal();
                {
                    GUI.color = Color.grey;
                    GUILayout.Box(entry.State.NetworkId.ToString(), s_NetIdWidth);
                    GUI.color = Color.white;
                    GUILayout.Box(entry.State.Username.Value.Value, s_UsernameWidth);
                }
                GUILayout.EndHorizontal();
                if (IsGuiFullyOffScreen()) break;
            }
        }

        private bool IsGuiFullyOffScreen()
        {
            var min = GUILayoutUtility.GetLastRect().min;
            return min.x > m_ScreenWidth || min.y > m_ScreenHeight;
        }

        void DrawNotification(PlayerListNotificationBuffer entry)
        {
            string notificationText;
            var username = entry.Event.Username.Value;
            Color color;
            switch (entry.Event.ChangeType)
            {
                case PlayerListEntry.ChangedRpc.UpdateType.PlayerDisconnect:
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
                        case NetworkStreamDisconnectReason.AuthenticationFailure:
                            notificationText = $"{username} could not be authenticated!";
                            color = new Color(0.39f, 0.01f, 0.63f);
                            break;
                        case NetworkStreamDisconnectReason.ProtocolError:
                            notificationText = $"{username} had a low-level transport error!";
                            color = new Color(0.39f, 0.01f, 0.63f);
                            break;
                        default:
                            notificationText = $"{username} disconnected with error {(int) entry.Event.Reason}!";
                            color = new Color(0.39f, 0.01f, 0.63f);
                            break;
                    }
                    break;
                case PlayerListEntry.ChangedRpc.UpdateType.NewJoiner:
                    notificationText = $"{username} connected!";
                    color = Color.green;
                    break;
                case PlayerListEntry.ChangedRpc.UpdateType.ExistingPlayer:
                    notificationText = $"{username} already here!";
                    color = new Color(0f, 0.89f, 1f);
                    break;
                case PlayerListEntry.ChangedRpc.UpdateType.UsernameChange:
                    notificationText = $"{username} changed names!";
                    color = Color.white;
                    break;
                default:
                    notificationText = $"{username} made unrecognised change {entry.Event.ChangeType}!";
                    color = Color.red;
                    break;
            }

            var targetAlpha = notificationOpacityCurve.Evaluate(entry.DurationLeft);
            GUI.color = Color.LerpUnclamped(Color.clear, color, targetAlpha);
            GUILayout.Box(notificationText, s_NotificationWidth);
        }
    }
}
