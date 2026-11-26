using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    /// <summary>
    /// Generic spawner that will setup ownership correctly if your ghost can have owners
    /// </summary>
    public class DelayedSpawner : MonoBehaviour
    {
        public GameObject DelayedSpawnPrefab;
        private readonly List<GhostObject> m_instantiatedObjects = new();
        private readonly List<GhostObject> m_destroyedObjects = new();

        private void OnGUI()
        {
            if (!Netcode.IsServerRole)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 300, 800));
            if (GUILayout.Button("Instantiate"))
            {
                var instance = Instantiate(DelayedSpawnPrefab, transform.position, transform.rotation);
                instance.name = m_instantiatedObjects.Count.ToString();
                instance.GetComponentInChildren<ObjectLabel>().UpdateStatus(ObjectLabel.SpawnStatus.Spawned);
                m_instantiatedObjects.Add(instance.GetComponent<GhostObject>());
            }

            foreach (var ghostObject in m_instantiatedObjects)
            {
                if (ghostObject.DelaySpawning)
                {
                    if (!ghostObject.IsSpawned)
                    {
                        if (GUILayout.Button($"Spawn {ghostObject.name}"))
                        {
                            ghostObject.Spawn();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button($"Despawn {ghostObject.name}"))
                        {
                            ghostObject.Despawn();
                        }
                    }
                }

                if (GUILayout.Button($"Destroy {ghostObject.name}"))
                {
                    Destroy(ghostObject.gameObject);
                    m_destroyedObjects.Add(ghostObject);
                }
            }

            foreach (var ghostObject in m_destroyedObjects)
            {
                m_instantiatedObjects.Remove(ghostObject);
            }
            m_destroyedObjects.Clear();
            GUILayout.EndArea();
        }
    }
}
