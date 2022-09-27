using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace HelloCube
{
    // An authoring component is just a normal MonoBehavior.
    [AddComponentMenu("HelloCube/Rotation Speed")]
    public class RotationSpeedAuthoring : MonoBehaviour
    {
        public float DegreesPerSecond = 360.0f;

        // In baking, this Baker will run once for every RotationSpeedAuthoring instance in an entity subscene.
        // (Nesting an authoring component's Baker class is simply an optional matter of style.)
        public class Baker : Baker<RotationSpeedAuthoring>
        {
            public override void Bake(RotationSpeedAuthoring authoring)
            {
                var data = new RotationSpeed
                {
                    RadiansPerSecond = math.radians(authoring.DegreesPerSecond)
                };
                AddComponent(data);
            }
        }
    }

    struct RotationSpeed : IComponentData
    {
        public float RadiansPerSecond;
    }
}
