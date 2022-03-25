using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(SpawnCopiesAsChildren))]
class SpawnCopiesAsChildrenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var spawner = (SpawnCopiesAsChildren)target;

        GUI.enabled = false;
        EditorGUILayout.FloatField("X * Z", spawner.CountX * spawner.CountZ);
        EditorGUILayout.FloatField("Current childCount", spawner.transform.childCount);
        GUI.enabled = true;

        var theSpawnerRoot = spawner.gameObject;

        if (spawner.Prefab == null)
        {
            EditorGUILayout.HelpBox("Specify the Prefab that needs to be cloned.", MessageType.Warning);
        }

        GUILayout.BeginHorizontal();

        var childCount = theSpawnerRoot.transform.childCount;
        GUI.enabled = childCount > 0;
        if (GUILayout.Button("Delete all children"))
        {
            for (var i = childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(theSpawnerRoot.transform.GetChild(i).gameObject);
            }

            EditorSceneManager.MarkSceneDirty(theSpawnerRoot.scene);
        }

        GUI.enabled = true;

        GUI.enabled = spawner.Prefab != null;
        if (GUILayout.Button("Spawn new children"))
        {
            var x0 = -spawner.CountX / 2;
            var z0 = -spawner.CountZ / 2;

            var allRenderers = spawner.Prefab.GetComponentsInChildren<Renderer>();
            var bounds = allRenderers[0].bounds;
            foreach (var r in allRenderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            for (var x = 0; x < spawner.CountX; x++)
            {
                for (var z = 0; z < spawner.CountZ; z++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(spawner.Prefab);
                    instance.name = spawner.Prefab.name + $"_{x}-{z}";

                    var vertNoise = noise.cnoise(new float2(x + x0, z + z0) * 0.36f);
                    instance.transform.position = theSpawnerRoot.transform.TransformPoint(
                        new float3((x + x0) * bounds.size.x * 1.1f, vertNoise * spawner.MaxVerticalOffset, (z + z0) * bounds.size.z * 1.1f));
                    instance.transform.parent = theSpawnerRoot.transform;

                    var localScale = instance.transform.localScale;
                    var heightNoise = noise.cnoise(new float2(x + x0, z + z0) * 0.9f);
                    var heightMultiplier = 0.5f * (1 + heightNoise) * spawner.MaxHeightMultiplier;
                    if (Mathf.Approximately(spawner.MaxHeightMultiplier, 1) && heightMultiplier < 1)
                    {
                        heightMultiplier = 1;
                    }
                    instance.transform.localScale = new Vector3(localScale.x, localScale.y * heightMultiplier, localScale.z);
                }
            }

            EditorSceneManager.MarkSceneDirty(theSpawnerRoot.scene);
        }
        GUI.enabled = true;

        GUILayout.EndHorizontal();
    }
}
