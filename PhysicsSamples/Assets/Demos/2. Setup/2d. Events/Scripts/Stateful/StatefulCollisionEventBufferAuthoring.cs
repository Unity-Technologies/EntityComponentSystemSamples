using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Stateful
{
    public struct StatefulCollisionEventDetails : IComponentData
    {
        public bool CalculateDetails;
    }

    public class StatefulCollisionEventBufferAuthoring : MonoBehaviour
    {
        [Tooltip("If selected, the details will be calculated in collision event dynamic buffer of this entity")]
        public bool CalculateDetails = false;

        class StatefulCollisionEventBufferBaker : Baker<StatefulCollisionEventBufferAuthoring>
        {
            public override void Bake(StatefulCollisionEventBufferAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (authoring.CalculateDetails)
                {
                    var dynamicBufferTag = new StatefulCollisionEventDetails
                    {
                        CalculateDetails = authoring.CalculateDetails
                    };

                    AddComponent(entity, dynamicBufferTag);
                }
                AddBuffer<StatefulCollisionEvent>(entity);
            }
        }
    }
}
