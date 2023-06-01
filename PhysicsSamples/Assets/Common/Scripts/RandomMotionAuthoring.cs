using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Common.Scripts
{
    // This behavior will set a dynamic body's linear velocity to get to randomly selected
    // point in space. When the body gets with a specified tolerance of the random position,
    // a new random position is chosen and the body starts header there instead.
    public class RandomMotionAuthoring : MonoBehaviour
    {
        public float3 Range = new float3(1);

        class Baker : Baker<RandomMotionAuthoring>
        {
            public override void Bake(RandomMotionAuthoring authoring)
            {
                var length = math.length(authoring.Range);
                var transform = GetComponent<Transform>();
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RandomMotion
                {
                    InitialPosition = transform.position,
                    DesiredPosition = transform.position,
                    Speed = length * 0.001f,
                    Tolerance = length * 0.1f,
                    Range = authoring.Range,
                });
            }
        }
    }

    public struct RandomMotion : IComponentData
    {
        public float CurrentTime;
        public float3 InitialPosition;
        public float3 DesiredPosition;
        public float Speed;
        public float Tolerance;
        public float3 Range;
    }
}
