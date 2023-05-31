using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PlanetGravity
{
    public class AsteroidAuthoring : MonoBehaviour
    {
        public float GravitationalMass;
        public float GravitationalConstant;
        public float EventHorizonDistance;
        public float RotationMultiplier;

        class Baker : Baker<AsteroidAuthoring>
        {
            public override void Bake(AsteroidAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Asteroid
                {
                    GravitationalCenter = GetComponent<Transform>().position,
                    GravitationalMass = authoring.GravitationalMass,
                    GravitationalConstant = authoring.GravitationalConstant,
                    EventHorizonDistance = authoring.EventHorizonDistance,
                    RotationMultiplier = authoring.RotationMultiplier
                });
            }
        }
    }

    public struct Asteroid : IComponentData
    {
        public float3 GravitationalCenter;
        public float GravitationalMass;
        public float GravitationalConstant;
        public float EventHorizonDistance;
        public float RotationMultiplier;
    }
}
