#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.Entities.Content;
using UnityEditor;
using UnityEngine;

namespace Streaming.RuntimeContentManager
{
    static class EditorUtility
    {
        [MenuItem("Assets/Spawn Area Prefab", true)]
        public static bool CreateStuffValidation()
        {
            return Selection.activeObject is DefaultAsset;
        }

        [MenuItem("Assets/Spawn Area Prefab")]
        public static void CreateStuff()
        {
            var folder = AssetDatabase.GetAssetPath(Selection.activeObject as DefaultAsset);
            var go = new GameObject(Selection.activeObject.name);
            var prefab =
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, folder + ".prefab", InteractionMode.AutomatedAction);
            var collider = prefab.AddComponent<BoxCollider>();
            collider.size = new Vector3(100, 30, 100);
            var dec = prefab.AddComponent<SpawnAreaAuthoring>();
            var objs = AssetDatabase.FindAssets("t:Mesh", new string[] { folder })
                .SelectMany(p => AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GUIDToAssetPath(p)))
                .ToArray();
            dec.meshes = new List<WeakObjectReference<Mesh>>(objs.Length);
            dec.materials = new List<WeakObjectReference<Material>>(objs.Length);
            dec.spacing = 15;

            for (int i = 0; i < objs.Length; i++)
            {
                var o = objs[i] as GameObject;
                if (o == null)
                {
                    continue;
                }
                var mf = o.GetComponent<MeshFilter>();
                var mr = o.GetComponent<MeshRenderer>();
                if (mf == null || mr == null || mf.sharedMesh == null || mr.sharedMaterial == null)
                {
                    continue;
                }

                if (mr.sharedMaterial.shader.name.Contains("Error"))
                {
                    continue;
                }

                var ms = mf.sharedMesh.bounds.size;
                if (ms.x > 10 || ms.y > 10 || ms.z > 10)
                {
                    continue;
                }

                dec.meshes.Add(new WeakObjectReference<Mesh>(mf.sharedMesh));
                dec.materials.Add(new WeakObjectReference<Material>(mr.sharedMaterial));
            }

            PrefabUtility.SavePrefabAsset(prefab);
            Object.DestroyImmediate(go);
        }
    }
}
#endif
