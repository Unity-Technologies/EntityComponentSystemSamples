using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace HelloCube.EnableableComponents
{
    public class RotationSpeedAuthoring : MonoBehaviour
    {
        public bool StartEnabled;
        public float DegreesPerSecond = 360.0f;

        public class Baker : Baker<RotationSpeedAuthoring>
        {
            public override void Bake(RotationSpeedAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new RotationSpeed { RadiansPerSecond = math.radians(authoring.DegreesPerSecond) });
                SetComponentEnabled<RotationSpeed>(entity, authoring.StartEnabled);
            }
        }
    }

    struct RotationSpeed : IComponentData, IEnableableComponent
    {
        public float RadiansPerSecond;
    }
}
