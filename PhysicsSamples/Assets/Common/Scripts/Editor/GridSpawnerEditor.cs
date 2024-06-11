using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridSpawner))]
public class GridSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GridSpawner spawner = (GridSpawner)target;

        if (GUILayout.Button("Spawn"))
        {
            spawner.Spawn();
        }
    }
}
