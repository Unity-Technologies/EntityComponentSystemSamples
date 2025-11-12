using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Tests
{
    class MotorDataAuthoring : MonoBehaviour
    {
        public bool EnableValidation = false;
        public float StartTime = 0.0f;
    }

    class MotorDataAuthoringBaker : Baker<MotorDataAuthoring>
    {
        public override void Bake(MotorDataAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MotorValidationSettings()
            {
                EnableValidation = authoring.EnableValidation,
                StartTime = authoring.StartTime
            });
        }
    }

    public struct MotorValidationSettings : IComponentData
    {
        public bool EnableValidation;
        public float StartTime;
    }

    public struct QuantitativeData : IComponentData
    {
        public int FrameCounter;
        public bool AccumulateData;
        public bool DataWritten;
    }
}
