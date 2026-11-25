using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    /// <summary>
    /// Spawns a grid of orbiting cubes around this spawner, in the XY plane. Neighboring cubes start at different points
    /// on their circle, so the whole field moves in a predictable wave the player can walk through.
    /// </summary>
    public class EnvironmentSpawner : MonoBehaviour
    {
        [SerializeField]
        GameObject m_ToSpawn;
        [SerializeField]
        int m_Columns = 6;
        [SerializeField]
        int m_Rows = 4;
        [SerializeField]
        float m_Spacing = 3;
        [SerializeField]
        float m_PhaseOffsetPerCell = 0.5f; // radians
        bool m_Spawned;

        void Awake()
        {
            if (!Netcode.IsServerRole)
                return;
            // Same pattern as GameObjectSpawner: a host's local connection is already connected, so it never gets OnConnect
            if (Netcode.IsHostRole)
                Spawn();
            else
                Netcode.OnConnectionEvent += OnConnect;
        }

        void OnDestroy()
        {
            Netcode.OnConnectionEvent -= OnConnect;
        }

        void OnConnect(Connection connection, NetcodeConnectionEvent connectionEvent)
        {
            // Ghosts can only be spawned once the server world is up, the first connection is a convenient signal for that
            if (connectionEvent.State != ConnectionState.State.Connected || !connection.World.IsServer())
                return;
            Spawn();
        }

        void Spawn()
        {
            if (m_Spawned)
                return;
            m_Spawned = true;

            var origin = transform.position - new Vector3((m_Columns - 1) * m_Spacing, (m_Rows - 1) * m_Spacing, 0f) * 0.5f;
            for (int row = 0; row < m_Rows; row++)
            {
                for (int column = 0; column < m_Columns; column++)
                {
                    var center = origin + new Vector3(column * m_Spacing, row * m_Spacing, 0f);
                    var spawned = Instantiate(m_ToSpawn, center, Quaternion.identity);
                    spawned.GetComponent<MovementBehaviour>().SetOrbit(center, (column + row) * m_PhaseOffsetPerCell);
                }
            }
        }
    }
}
