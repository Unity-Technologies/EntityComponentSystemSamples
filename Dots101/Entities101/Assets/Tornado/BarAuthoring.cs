using UnityEngine;
using Unity.Entities;

namespace Tutorials.Tornado
{
    public class BarAuthoring : MonoBehaviour
    {
        class Baker : Baker<BarAuthoring>
        {
            public override void Bake(BarAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Bar>(entity);
                AddComponent<BarThickness>(entity);
            }
        }
    }

    public struct Bar : IComponentData
    {
        public int pointA;
        public int pointB;
        public float length;
    }

    public struct BarThickness : IComponentData
    {
        public float Value;
    }
}
