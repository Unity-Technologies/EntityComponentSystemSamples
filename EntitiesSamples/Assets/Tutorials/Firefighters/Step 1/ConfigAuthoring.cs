using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Tutorials.Firefighters
{
    public class ConfigAuthoring : MonoBehaviour
    {
        [Header("Ponds")] 
        public int NumPondsPerEdge;

        [Header("Bots")] 
        public int NumTeams;
        public int NumPassersPerTeam;
        public int BotMoveSpeed = 3; // units per second
        public float LineMaxOffset = 4;

        [Header("Buckets")] 
        public float BucketFillRate;
        public int NumBuckets;
        public Color BucketEmptyColor;
        public Color BucketFullColor;
        public float BucketEmptyScale;
        public float BucketFullScale;

        [Header("Ground")] 
        public int GroundNumColumns;
        public int GroundNumRows;

        [Header("Heat")] 
        public Color MinHeatColor;
        public Color MaxHeatColor;
        public float HeatSpreadSpeed;
        public float HeatOscillationScale;
        public int NumInitialCellsOnFire;
        public float HeatDouseTargetMin;

        [Header("Prefabs")] 
        public GameObject BotPrefab;
        public GameObject BucketPrefab;
        public GameObject PondPrefab;
        public GameObject GroundCellPrefab;
        public GameObject BotAnimatedPrefabGO;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                var numTeams = math.max(authoring.NumTeams, 1);
                AddComponent(entity, new Config
                {
                    GroundNumColumns = authoring.GroundNumColumns,
                    GroundNumRows = authoring.GroundNumRows,
                    NumPondsPerEdge = authoring.NumPondsPerEdge,
                    NumTeams = numTeams,
                    NumPassersPerTeam =
                        (math.max(authoring.NumPassersPerTeam, 4) / 2) *
                        2, // round down to even number and set min to 4
                    BotMoveSpeed = authoring.BotMoveSpeed,
                    LineMaxOffset = authoring.LineMaxOffset,
                    NumBuckets =
                        math.max(authoring.NumBuckets, numTeams), // make sure there's at least one bucket per team
                    BucketFillRate = authoring.BucketFillRate,
                    MinHeatColor = (Vector4)authoring.MinHeatColor,
                    MaxHeatColor = (Vector4)authoring.MaxHeatColor,
                    BucketEmptyColor = (Vector4)authoring.BucketEmptyColor,
                    BucketFullColor = (Vector4)authoring.BucketFullColor,
                    BucketEmptyScale = authoring.BucketEmptyScale,
                    BucketFullScale = authoring.BucketFullScale,
                    NumInitialCellsOnFire = authoring.NumInitialCellsOnFire,
                    HeatSpreadSpeed = authoring.HeatSpreadSpeed,
                    HeatDouseTargetMin = authoring.HeatDouseTargetMin,
                    HeatOscillationScale = authoring.HeatOscillationScale,
                    GroundCellYScale = authoring.GroundCellPrefab.transform.localScale.y,
                    BotPrefab = GetEntity(authoring.BotPrefab, TransformUsageFlags.Dynamic),
                    BucketPrefab = GetEntity(authoring.BucketPrefab, TransformUsageFlags.Dynamic),
                    PondPrefab = GetEntity(authoring.PondPrefab, TransformUsageFlags.Dynamic),
                    GroundCellPrefab = GetEntity(authoring.GroundCellPrefab, TransformUsageFlags.Dynamic),
                });
                var configManaged = new ConfigManaged();
                configManaged.BotAnimatedPrefabGO = authoring.BotAnimatedPrefabGO;
                AddComponentObject(entity, configManaged);
            }
        }
    }

    public struct Config : IComponentData
    {
        public int GroundNumColumns;
        public int GroundNumRows;
        public int NumPondsPerEdge;
        public int NumTeams;
        public int NumPassersPerTeam;
        public int BotMoveSpeed;
        public float LineMaxOffset;
        public int NumBuckets;

        public float4 MinHeatColor;
        public float4 MaxHeatColor;
        public float HeatSpreadSpeed;
        public float HeatDouseTargetMin;
        public float HeatOscillationScale;
        public int NumInitialCellsOnFire;
        public float GroundCellYScale;

        public float4 BucketEmptyColor;
        public float4 BucketFullColor;
        public float BucketEmptyScale;
        public float BucketFullScale;
        public float BucketFillRate;

        public Entity BotPrefab;
        public Entity BucketPrefab;
        public Entity PondPrefab;
        public Entity GroundCellPrefab;
    }

    public class ConfigManaged : IComponentData
    {
        public GameObject BotAnimatedPrefabGO;
        public UIController UIController;
    }
}