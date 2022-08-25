using UnityEngine;

class SpawnCopiesAsChildren : MonoBehaviour
{
    public GameObject Prefab;
    public int CountX = 10;
    public int CountZ = 10;

    public float MaxVerticalOffset = 3;
    public float MaxHeightMultiplier = 3;

    void OnValidate()
    {
        if (CountX < 0)
            CountX = 0;

        if (CountZ < 0)
            CountZ = 0;

        if (MaxVerticalOffset < 0)
            MaxVerticalOffset = 0;

        if (MaxHeightMultiplier < 0)
            MaxHeightMultiplier = 0;
    }
}
