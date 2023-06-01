using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Events
{
    public class TriggerVolumeForceFieldAuthoring : MonoBehaviour
    {
        public enum Direction
        {
            Center,
            XAxis,
            YAxis,
            ZAxis
        };

        public float Strength = 10f;
        public float DeadZone = 0.5f;
        public Direction Axis = Direction.Center;
        public float Rotation = 0;
        public bool Proportional = true;
        public bool MassInvariant = false;

        class Baker : Baker<TriggerVolumeForceFieldAuthoring>
        {
            public override void Bake(TriggerVolumeForceFieldAuthoring authoring)
            {
                var transform = GetComponent<Transform>();
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TriggerVolumeForceField
                {
                    Center = transform.position,
                    Strength = authoring.Strength,
                    DeadZone = (authoring.DeadZone == 0) ? 0.001f : math.abs(authoring.DeadZone),
                    Axis = (int)authoring.Axis - 1,
                    Rotation = math.radians(authoring.Rotation),
                    Proportional = authoring.Proportional ? 1 : 0,
                    MassInvariant = authoring.MassInvariant ? 1 : 0
                });
            }
        }
    }

    public struct TriggerVolumeForceField : IComponentData
    {
        public float3 Center;
        public float DeadZone;
        public float Strength;
        public float Rotation;
        public int Axis;
        public int Proportional;
        public int MassInvariant;
    }
}
