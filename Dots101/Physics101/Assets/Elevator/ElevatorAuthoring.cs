using UnityEngine;
using Unity.Entities;

namespace Elevator
{
    public class ElevatorAuthoring : MonoBehaviour
    {
        public float Speed;
        public float MaxHeight;
        public float MinHeight;
        
        class Baker : Baker<ElevatorAuthoring>
        {
            public override void Bake(ElevatorAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new Elevator
                {
                    Speed = authoring.Speed,
                    MaxHeight = authoring.MaxHeight,
                    MinHeight = authoring.MinHeight,
                });
            }
        }
    }

    public struct Elevator : IComponentData
    {
        public float Speed;   // meters per second
        public float MaxHeight;
        public float MinHeight;
    }
}