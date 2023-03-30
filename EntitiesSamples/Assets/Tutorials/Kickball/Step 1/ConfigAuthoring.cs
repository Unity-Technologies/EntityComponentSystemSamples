using Unity.Entities;
using UnityEngine;

namespace Tutorials.Kickball.Step1
{
    // The Config component will be used as a singleton (meaning only one entity will have this component).
    // It stores a grab bag of game parameters plus the entity prefabs that we'll instantiate at runtime.

    public class ConfigAuthoring : MonoBehaviour
    {
        // Most of these fields are unused in Step 1, but they will be used in later steps.
        public int ObstaclesNumRows;
        public int ObstaclesNumColumns;
        public float ObstacleGridCellSize;
        public float ObstacleRadius;
        public float ObstacleOffset;
        public float PlayerOffset;
        public float PlayerSpeed;
        public float BallStartVelocity;
        public float BallVelocityDecay;
        public float BallKickingRange;
        public float BallKickForce;
        public GameObject ObstaclePrefab;
        public GameObject PlayerPrefab;
        public GameObject BallPrefab;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                // Each authoring field corresponds to a component field of the same name.
                AddComponent(entity, new Config
                {
                    NumRows = authoring.ObstaclesNumRows,
                    NumColumns = authoring.ObstaclesNumColumns,
                    ObstacleGridCellSize = authoring.ObstacleGridCellSize,
                    ObstacleRadius = authoring.ObstacleRadius,
                    ObstacleOffset = authoring.ObstacleOffset,
                    PlayerOffset = authoring.PlayerOffset,
                    PlayerSpeed = authoring.PlayerSpeed,
                    BallStartVelocity = authoring.BallStartVelocity,
                    BallVelocityDecay = authoring.BallVelocityDecay,
                    BallKickingRangeSQ = authoring.BallKickingRange * authoring.BallKickingRange,
                    BallKickForce = authoring.BallKickForce,
                    // GetEntity() bakes a GameObject prefab into its entity equivalent.
                    ObstaclePrefab = GetEntity(authoring.ObstaclePrefab, TransformUsageFlags.Dynamic),
                    PlayerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic),
                    BallPrefab = GetEntity(authoring.BallPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public int NumRows; // obstacles and players spawns in a grid, one obstacle and player per cell
        public int NumColumns;
        public float ObstacleGridCellSize;
        public float ObstacleRadius;
        public float ObstacleOffset;
        public float PlayerOffset;
        public float PlayerSpeed; // meters per second
        public float BallStartVelocity;
        public float BallVelocityDecay;
        public float BallKickingRangeSQ; // square distance of how close a player must be to a ball to kick it
        public float BallKickForce;
        public Entity ObstaclePrefab;
        public Entity PlayerPrefab;
        public Entity BallPrefab;
    }
}
