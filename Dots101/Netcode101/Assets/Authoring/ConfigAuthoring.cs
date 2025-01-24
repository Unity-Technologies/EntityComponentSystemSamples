using System;
using UnityEngine;
using Unity.Entities;

namespace KickBall
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public ConfigScriptableObject ConfigSO;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                DependsOn(authoring.ConfigSO);  // ensures that baking will be re-triggered if the SO is modified

                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                AddComponent(entity, new EntityPrefabs
                {
                    Obstacle = GetEntity(authoring.ConfigSO.ObstaclePrefab, TransformUsageFlags.Dynamic),
                    Player = GetEntity(authoring.ConfigSO.PlayerPrefab, TransformUsageFlags.Dynamic),
                    Ball = GetEntity(authoring.ConfigSO.BallPrefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, authoring.ConfigSO.Player);
                AddComponent(entity, authoring.ConfigSO.Obstacle);
                AddComponent(entity, authoring.ConfigSO.Ball);
            }
        }
    }

    [Serializable]
    public struct EntityPrefabs : IComponentData
    {
        public Entity Obstacle;
        public Entity Player;
        public Entity Ball;
    }

    [Serializable]
    public struct PlayerConfig : IComponentData
    {
        public float Speed;
    }

    [Serializable]
    public struct BallConfig : IComponentData
    {
        public float SpawnHeight;
        public float KickingRangeSQ;
        public float KickForce;
    }

    [Serializable]
    public struct ObstacleConfig : IComponentData
    {
        public int NumRows;
        public int NumColumns;
        public float GridCellSize;
        public float Offset;
    }
}