using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ActivationPlates
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject SpawnPrefab;
        public Transform SpawnPoint;
        public float ContinuousRepetitionInterval = 0.5f;
        public Color InactiveColor;
        public Color ActiveColor;
        public float PlayerMoveSpeed = 2;
        
        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);

                authoring.SpawnPrefab.transform.localPosition = authoring.SpawnPoint.localPosition;

                AddComponent(entity, new Config
                {
                    SpawnPrefab = GetEntity(authoring.SpawnPrefab, TransformUsageFlags.Dynamic),
                    ContinuousRepetitionInterval = authoring.ContinuousRepetitionInterval,
                    InactiveColor = (Vector4)authoring.InactiveColor,
                    ActiveColor = (Vector4)authoring.ActiveColor,
                    PlayerMoveSpeed = authoring.PlayerMoveSpeed
                });
            }
        }
    }
    
    public struct Config : IComponentData
    {
        public Entity SpawnPrefab;
        public float ContinuousRepetitionInterval;
        public float4 InactiveColor;
        public float4 ActiveColor;
        public float PlayerMoveSpeed;
    }
}