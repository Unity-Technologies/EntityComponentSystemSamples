using UnityEngine;

public class SpawnGameObjectPrefabs : MonoBehaviour
{
#pragma warning disable 649
    public GameObject prefab;
    public Vector3 range = new Vector3(10f, 10f, 10f);
    public int count;
#pragma warning restore 649

    // Start is called before the first frame update
    void Start()
    {
        var positions = new Vector3[count];
        var rotations = new Quaternion[count];

        RandomPointsOnCircle(transform.position, range, ref positions, ref rotations);

        for( int i = 0; i < count; i++)
        {
            var instance = Instantiate(prefab);
            instance.transform.parent = this.transform;
            instance.transform.position = positions[i];
            instance.transform.rotation = rotations[i];
        }
    }


    protected static void RandomPointsOnCircle(Vector3 center, Vector3 range, ref Vector3[] positions, ref Quaternion[] rotations, int seed = 0)
    {
        var count = positions.Length;
        // initialize the seed of the random number generator
        Random.InitState(seed);
        for (int i = 0; i < count; i++)
        {
            positions[i] = center + new Vector3(Random.Range(-range.x, range.x), Random.Range(-range.y, range.y), Random.Range(-range.z, range.z));
            rotations[i] = Random.rotation;
        }
    }
}
