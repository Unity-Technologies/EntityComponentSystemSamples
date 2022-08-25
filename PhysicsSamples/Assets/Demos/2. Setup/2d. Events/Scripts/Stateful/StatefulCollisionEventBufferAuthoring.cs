using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Stateful
{
    public struct StatefulCollisionEventDetails : IComponentData
    {
        public bool CalculateDetails;
    }

    public class StatefulCollisionEventBufferAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Tooltip("If selected, the details will be calculated in collision event dynamic buffer of this entity")]
        public bool CalculateDetails = false;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (CalculateDetails)
            {
                var dynamicBufferTag = new StatefulCollisionEventDetails
                {
                    CalculateDetails = CalculateDetails
                };

                dstManager.AddComponentData(entity, dynamicBufferTag);
            }
            dstManager.AddBuffer<StatefulCollisionEvent>(entity);
        }
    }
}
