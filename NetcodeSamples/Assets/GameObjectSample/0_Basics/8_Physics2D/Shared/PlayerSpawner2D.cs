#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    // Spawns one player per connection, as GameObjectSpawner does, but spread along the floor so they don't
    // start overlapping.
    public class PlayerSpawner2D : MonoBehaviour
    {
        [SerializeField] GameObject m_PlayerPrefab;
        [SerializeField] float m_Spacing = 1.5f;

        readonly Dictionary<int, GameObject> m_Players = new Dictionary<int, GameObject>();

        void Awake()
        {
            Netcode.OnConnectionEvent += OnConnectionEvent;
            if (Netcode.IsHostRole)
                SpawnPlayer(Netcode.LocalConnection);
        }

        void OnDestroy()
        {
            Netcode.OnConnectionEvent -= OnConnectionEvent;
        }

        void OnConnectionEvent(Connection connection, NetcodeConnectionEvent @event)
        {
            if (@event.State == ConnectionState.State.Connected && connection.World.IsServer())
                SpawnPlayer(connection);
            else if (@event.State == ConnectionState.State.Disconnected && connection.World.IsServer())
            {
                if (m_Players.Remove(connection.NetworkId.Value, out var player) && player != null)
                    Destroy(player);
            }
        }

        void SpawnPlayer(Connection connection)
        {
            var id = connection.NetworkId.Value;
            if (!StartChecks.Passed || m_Players.ContainsKey(id))
                return;

            var position = transform.position + Vector3.right * (m_Spacing * (id - 1));
            var player = Instantiate(m_PlayerPrefab, position, Quaternion.identity);
            player.name = connection.World.IsHost() ? $"Player {id}" : $"Player {id} (server)";
            player.GetComponent<GhostObject>().OwnerNetworkId = connection.NetworkId;
            m_Players.Add(id, player);
        }
    }
}
#endif
