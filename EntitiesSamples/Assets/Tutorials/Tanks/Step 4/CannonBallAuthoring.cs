using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Tutorials.Tanks.Step4
{
    public class CannonBallAuthoring : MonoBehaviour
    {
        class Baker : Baker<CannonBallAuthoring>
        {
            public override void Bake(CannonBallAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // By default, components are zero-initialized,
                // so the Velocity field of CannonBall will be float3.zero.
                AddComponent<CannonBall>(entity);

                // Used in Step 8.
                AddComponent<URPMaterialPropertyBaseColor>(entity);
            }
        }
    }

    // Like for tanks, we are creating a component to identify the cannon ball entities.
    public struct CannonBall : IComponentData
    {
        public float3 Velocity; // Used in a later step.
    }
}
