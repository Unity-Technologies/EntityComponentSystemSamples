using UnityEngine;
using Unity.Entities;

namespace GravityWell
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject BallPrefab;
        public int BallCount = 20;
        public float WellOrbitRadius = 6;
        public float WellOrbitSpeed = 1; // radians per second
        public float WellStrength = 100;

        public class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(entity, new Config
                {
                    BallPrefab = GetEntity(authoring.BallPrefab, TransformUsageFlags.Dynamic),
                    BallCount = authoring.BallCount,
                    WellOrbitRadius = authoring.WellOrbitRadius,
                    WellOrbitSpeed = authoring.WellOrbitSpeed,
                    WellStrength = authoring.WellStrength,
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity BallPrefab;
        public int BallCount;
        public float WellOrbitRadius;
        public float WellOrbitSpeed; // radians per second
        public float WellStrength;
    }
}