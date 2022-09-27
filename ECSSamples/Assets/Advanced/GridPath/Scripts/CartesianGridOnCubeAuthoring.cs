using Unity.Entities;
using UnityEngine;

// ReSharper disable once InconsistentNaming
[AddComponentMenu("DOTS Samples/GridPath/Cartesian Grid on Cube")]
public class CartesianGridOnCubeAuthoring : MonoBehaviour
{
    [Range(2, 512)]
    public int RowCount;
    public GameObject[] FloorPrefab;
    public GameObject WallPrefab;

    // Specific wall probability, given PotentialWallProbability
    public float WallSProbability = 0.5f;
    public float WallWProbability = 0.5f;

    class Baker : Baker<CartesianGridOnCubeAuthoring>
    {
        public override void Bake(CartesianGridOnCubeAuthoring authoring)
        {
            var floorPrefabCount = authoring.FloorPrefab.Length;
            if (floorPrefabCount == 0)
                return;

            var floorPrefabs = AddBuffer<CartesianGridOnCubeGeneratorFloorPrefab>();
            floorPrefabs.Length = authoring.FloorPrefab.Length;

            for (int i = 0; i < authoring.FloorPrefab.Length; i++)
            {
                floorPrefabs[i] = GetEntity(authoring.FloorPrefab[i]);
            }

            AddComponent(new CartesianGridOnCubeGenerator
            {
                RowCount = authoring.RowCount,
                WallSProbability = authoring.WallSProbability,
                WallWProbability = authoring.WallWProbability,
                WallPrefab = GetEntity(authoring.WallPrefab),
            });
        }
    }
}
