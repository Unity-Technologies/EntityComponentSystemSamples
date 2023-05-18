using Tutorials.Kickball.Step2;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Tutorials.Kickball.Step3
{
    public class BallAuthoring : MonoBehaviour
    {
        class Baker : Baker<BallAuthoring>
        {
            public override void Bake(BallAuthoring authoring)
            {
               var entity = GetEntity(TransformUsageFlags.Dynamic);

                // A single authoring component can add multiple components to the entity.
                AddComponent<Ball>(entity);
                AddComponent<Velocity>(entity);

                // Used in Step 5
                AddComponent<Carry>(entity);
                SetComponentEnabled<Carry>(entity, false);
            }
        }
    }

    // A tag component for ball entities.
    public struct Ball : IComponentData
    {
    }

    // A 2d velocity vector for the ball entities.
    public struct Velocity : IComponentData
    {
        public float2 Value;
    }
}
