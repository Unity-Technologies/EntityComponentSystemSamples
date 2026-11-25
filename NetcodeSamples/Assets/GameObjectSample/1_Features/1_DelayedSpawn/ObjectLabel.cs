using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    public partial class ObjectLabel : GhostBehaviour
    {
        private TextMesh m_ObjectLabel;
        private MeshRenderer m_Renderer;
        private GhostField<SpawnStatus> m_SpawnStatus = new GhostField<SpawnStatus>();
        public SpawnStatus existingStatus;

        public enum SpawnStatus
        {
            Unspawned,
            Spawned,
            SomeOtherValue,
        }

        public void UpdateStatus(SpawnStatus spawnStatus)
        {
            m_SpawnStatus.Value = spawnStatus;
        }

        public void Update()
        {
            if (!Ghost.IsSpawned) {
                return;
            }
            var spawnStatus = m_SpawnStatus.Value;
            if (spawnStatus != existingStatus)
            {
                m_ObjectLabel.text = spawnStatus switch
                {
                    SpawnStatus.Spawned => "Spawned",
                    SpawnStatus.Unspawned => "Not Spawned",
                    _ => "Some text"
                };
                existingStatus = spawnStatus;
            }
        }

        public override void Awake()
        {
            if (m_ObjectLabel == null)
            {
                m_ObjectLabel = GetComponent<TextMesh>();
            }

            m_ObjectLabel.text = "Initial Status";
            existingStatus = SpawnStatus.Unspawned;

            base.Awake();
        }

        public override void OnGhostDespawn()
        {
            if (m_ObjectLabel == null)
            {
                m_ObjectLabel = GetComponent<TextMesh>();
            }

            m_ObjectLabel.text = "None";
            existingStatus = SpawnStatus.Unspawned;

            base.OnGhostDespawn();
        }
    }

}
