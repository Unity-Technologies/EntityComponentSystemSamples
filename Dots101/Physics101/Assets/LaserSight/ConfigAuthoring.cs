using Unity.Entities;
using UnityEngine;

namespace LaserSight
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public float PlayerMoveSpeed = 2;
        public float MaxLaserLength = 100f;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);

                AddComponent(entity, new Config
                {
                    PlayerMoveSpeed = authoring.PlayerMoveSpeed,
                    MaxLaserLength = authoring.MaxLaserLength
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public float PlayerMoveSpeed;
        public float MaxLaserLength;
    }
}