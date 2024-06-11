using Unity.Entities;
using UnityEngine;

namespace Unity.Physics
{
    /// <summary>
    /// A countdown timer for changing the collision filter on an entity. When the countdown expires, the collision
    /// filter is changed to the next filter in the list and the countdown is reset.
    /// </summary>
    public struct ChangeCollisionFilterCountdown : IComponentData
    {
        public int Countdown;
        internal int ResetCountdown;
    }

    /// <summary>
    /// A component that stores the material indices for the red, green and blue materials for mapping to the
    /// RenderMeshArray in a scene
    /// </summary>
    public struct ColoursForFilter : IComponentData
    {
        public int RedIndex;
        public int BlueIndex;
        public int GreenIndex;
    }

    public class RotateThroughCollisionFiltersAuthoring : MonoBehaviour
    {
        public int Countdown = 60;

        class RotateThroughCollisionFiltersBaker : Baker<RotateThroughCollisionFiltersAuthoring>
        {
            public override void Bake(RotateThroughCollisionFiltersAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ChangeCollisionFilterCountdown
                {
                    Countdown = authoring.Countdown,
                    ResetCountdown = authoring.Countdown
                });
            }
        }
    }
}
