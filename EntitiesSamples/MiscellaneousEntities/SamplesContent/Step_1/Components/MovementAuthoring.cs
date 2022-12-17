using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ClosestTarget
{
    public class MovementAuthoring : MonoBehaviour
    {
        class Baker : Baker<MovementAuthoring>
        {
            public override void Bake(MovementAuthoring authoring)
            {
                AddComponent<Movement>();
            }
        }
    }

    public struct Movement : IComponentData
    {
        public float2 Value;
    }
}