using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Samples.Common
{
    public class RandomSpawner : MonoBehaviour
    {
        public GameObject prefab;
        public int count = 10000;
        public float radius = 4.0F;
        public int transformsPerHierarchy = 500;
        public enum ActivateMode{ None, ActivateDeactivateAll }
        public ActivateMode activateMode;

        private List<GameObject> roots = new List<GameObject>();

        void OnEnable()
        {
            Profiler.BeginSample ("Spawn '" + prefab.name + "'");
            GameObject root = null;

            for (int i = 0; i != count; i++)
            {
                if (transformsPerHierarchy != 0 && i % transformsPerHierarchy == 0)
                {
                    root = new GameObject("Chunk "+i);
                    root.transform.hierarchyCapacity = transformsPerHierarchy;
                    roots.Add (root);
                }

                Instantiate (prefab, Random.insideUnitSphere * radius + transform.position, Random.rotation, root.transform);
            }

            Profiler.EndSample ();
        }

        void Update ()
        {
            if (activateMode == ActivateMode.ActivateDeactivateAll)
            {
                foreach (var go in roots)
                    go.SetActive (false);
                foreach (var go in roots)
                    go.SetActive (true);
            }
        }
    }
}
