using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Streaming.SceneManagement.Common
{
    public class OscillatingAuthoring : MonoBehaviour
    {
        public float amplitude;
        public float frequency;
        public float3 direction;
        public float offset;

        class Baker : Baker<OscillatingAuthoring>
        {
            public override void Bake(OscillatingAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);
                AddComponent(entity, new Oscillating
                {
                    Amplitude = authoring.amplitude,
                    Frequency = authoring.frequency,
                    Direction = authoring.direction,
                    Offset = authoring.offset,
                    Center = GetComponent<Transform>().position
                });
            }
        }
    }

    public struct Oscillating : IComponentData
    {
        public float Amplitude;
        public float Frequency;
        public float Offset;
        public float3 Direction;
        public float3 Center;
    }
}
