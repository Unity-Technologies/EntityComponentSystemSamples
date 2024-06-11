using UnityEditor;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public GameObject Prefab;
    public Vector3 spacing;
    public int NumX;
    public int NumY;
    public int NumZ;

    public void Spawn()
    {
#if UNITY_EDITOR
        for (int i = 0; i < NumX; ++i)
        {
            for (int j = 0; j < NumY; ++j)
            {
                for (int k = 0; k < NumZ; ++k)
                {
                    var pos = new Vector3(i * spacing[0], j * spacing[1], k * spacing[2]);
                    var go = PrefabUtility.InstantiatePrefab(Prefab, transform) as GameObject;
                    go.transform.localPosition = pos;
                }
            }
        }
#endif
    }
}
