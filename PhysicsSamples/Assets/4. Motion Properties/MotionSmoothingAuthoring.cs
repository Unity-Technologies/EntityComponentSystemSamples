using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MotionProperties
{
    // this script forces a lower fixed time step for both GO and DOTS physics to demonstrate motion smoothing
    public class MotionSmoothingAuthoring : MonoBehaviour
    {
        // default to a low tick rate for demonstration purposes
        [Min(0)] public int StepsPerSecond = 15;

        float m_FixedTimetep;

        void OnEnable()
        {
            m_FixedTimetep = Time.fixedDeltaTime;
            Time.fixedDeltaTime = 1f / StepsPerSecond;
        }

        void OnDisable() => Time.fixedDeltaTime = m_FixedTimetep;

        void OnValidate() => StepsPerSecond = math.max(0, StepsPerSecond);

        class Baker : Baker<MotionSmoothingAuthoring>
        {
            public override void Bake(MotionSmoothingAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new FixedStep
                {
                    Value = 1f / authoring.StepsPerSecond
                });
            }
        }
    }

    struct FixedStep : IComponentData
    {
        public float Value;
    }
}
