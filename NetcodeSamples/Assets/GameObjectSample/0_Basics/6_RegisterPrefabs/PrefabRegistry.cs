using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    [DefaultExecutionOrder(-100)]
    public class PrefabRegistry : MonoBehaviour
    {
        public List<GameObject> Prefabs;

        void Awake()
        {
            foreach (var prefab in Prefabs)
            {
                if (prefab.GetComponent<GhostObject>().SkipAutomaticPrefabRegistration)
                {
                    Debug.Log($"[PrefabRegistry] registering {prefab.name}");
                    Netcode.RegisterPrefab(prefab);
                }
                else
                {
                    Debug.Log($"[PrefabRegistry] not registering {prefab.name} as it auto registers");
                }
            }
        }
    }
}
