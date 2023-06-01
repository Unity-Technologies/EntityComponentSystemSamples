using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public class GravityWellAuthoring : MonoBehaviour
    {
        public float Strength = 100.0f;
        public float Radius = 10.0f;

        class Baker : Baker<GravityWellAuthoring>
        {
            public override void Bake(GravityWellAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GravityWell
                {
                    Strength = authoring.Strength,
                    Radius = authoring.Radius,
                    Position = GetComponent<Transform>().position
                });
            }
        }
    }


    public struct GravityWell : IComponentData
    {
        public float Strength;
        public float Radius;
        public float3 Position;
    }
}
