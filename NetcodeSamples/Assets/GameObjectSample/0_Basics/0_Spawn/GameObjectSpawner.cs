using System;
using UnityEngine.Assertions;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    /// <summary>
    /// Generic spawner that will setup ownership correctly if your ghost can have owners
    /// </summary>
    public class GameObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        GameObject prefab;

        void Awake()
        {
            Netcode.OnConnectionEvent += OnConnect;
            if (Netcode.IsHostRole)
            {
                SpawnPlayer(Netcode.LocalConnection);
            }
        }

        void SpawnPlayer(Connection connection)
        {
            var go = GameObject.Instantiate(prefab, transform.position, transform.rotation);
            go.transform.localScale = Vector3.Scale(prefab.transform.localScale, transform.localScale);
            void OnDisconnect(Connection disconnectedConnection, NetcodeConnectionEvent @event)
            {
                if (@event.State != ConnectionState.State.Disconnected)
                    return;
                if (disconnectedConnection.NetworkId == connection.NetworkId)
                {
                    Netcode.OnConnectionEvent -= OnDisconnect;
                    GameObject.Destroy(go);
                }
            }
            if (!connection.World.IsHost() || connection != Netcode.LocalConnection)
                Netcode.OnConnectionEvent += OnDisconnect;

            var ghostObject = go.GetComponent<GhostObject>();
            if (ghostObject.HasOwner)
                ghostObject.OwnerNetworkId = connection.NetworkId;
        }

        void OnConnect(Connection connection, NetcodeConnectionEvent @event)
        {
            if (@event.State != ConnectionState.State.Connected)
                return;
            if (!connection.World.IsServer()) return;

            SpawnPlayer(connection);
        }

        void OnDestroy()
        {
            Netcode.OnConnectionEvent -= OnConnect;
        }
    }
}
