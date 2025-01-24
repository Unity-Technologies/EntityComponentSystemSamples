using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BreakingBricks
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject BrickPrefab;
        public GameObject BallPrefab;
        public float BallSpawnInterval = 4f;
        public int NumBallsSpawn = 10;
        public int NumBricksSpawn = 10;
        public float ImpactStrength = 5f;
        public Color FullHitpointsColor = Color.green;
        public Color EmptyHitpointsColor = Color.red;
        public Bounds SpawnBounds;
        public float BallSpawnHeight; // how high the ball spawning bounds should be above the brick spawning bounds 
        public float BallDespawnHeight; // balls falling below this threshold are despawned

        public class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new Config
                {
                    BrickPrefab = GetEntity(authoring.BrickPrefab, TransformUsageFlags.Dynamic),
                    BallPrefab = GetEntity(authoring.BallPrefab, TransformUsageFlags.Dynamic),
                    BallSpawnInterval = authoring.BallSpawnInterval,
                    NumBallsSpawn = authoring.NumBallsSpawn,
                    NumBricksSpawn = authoring.NumBricksSpawn,
                    ImpactStrength = authoring.ImpactStrength,
                    FullHitpointsColor = (Vector4)authoring.FullHitpointsColor,
                    EmptyHitpointsColor = (Vector4)authoring.EmptyHitpointsColor,
                    SpawnBoundsMax = authoring.SpawnBounds.max,
                    SpawnBoundsMin = authoring.SpawnBounds.min,
                    BallSpawnHeight = authoring.BallSpawnHeight,
                    BallDespawnHeight = authoring.BallDespawnHeight
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity BrickPrefab;
        public Entity BallPrefab;
        public float BallSpawnInterval;
        public int NumBallsSpawn;
        public int NumBricksSpawn;
        public float ImpactStrength;
        public float4 FullHitpointsColor;
        public float4 EmptyHitpointsColor;
        public float3 SpawnBoundsMax;
        public float3 SpawnBoundsMin;
        public float BallSpawnHeight;
        public float BallDespawnHeight;
    }
}