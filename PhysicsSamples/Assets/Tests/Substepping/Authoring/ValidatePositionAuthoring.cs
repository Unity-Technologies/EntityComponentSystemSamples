using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Tests
{
    class ValidatePositionAuthoring : MonoBehaviour
    {
        public bool EnableValidation = false;
        public int AcquireDataSteps = 256;
    }

    class ValidatePositionAuthoringBaker : Baker<ValidatePositionAuthoring>
    {
        public override void Bake(ValidatePositionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ValidatePosition()
            {
                EnableValidation = authoring.EnableValidation,
                AcquireDataSteps = authoring.AcquireDataSteps
            });

            AddComponent(entity, new QuantitativeData
            {
                FrameCounter = 0,
                AccumulateData = true,
                DataWritten = false
            });
        }
    }

    public struct ValidatePosition : IComponentData
    {
        public bool EnableValidation;
        public int AcquireDataSteps;
    }
}
