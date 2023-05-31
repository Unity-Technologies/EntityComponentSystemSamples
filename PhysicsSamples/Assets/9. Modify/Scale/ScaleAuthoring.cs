using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Modify
{
    public class ScaleAuthoring : MonoBehaviour
    {
        [Range(0, 10)] public float Min = 0;
        [Range(0, 10)] public float Max = 10;

        class Baker : Baker<ScaleAuthoring>
        {
            public override void Bake(ScaleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.ManualOverride);
                AddComponent(entity, new Scaling
                {
                    Min = authoring.Min,
                    Max = authoring.Max,
                    Target = math.lerp(authoring.Min, authoring.Max, 0.5f),
                });
            }
        }
    }

    public struct Scaling : IComponentData
    {
        public float Min;
        public float Max;
        public float Target;
    }
}
