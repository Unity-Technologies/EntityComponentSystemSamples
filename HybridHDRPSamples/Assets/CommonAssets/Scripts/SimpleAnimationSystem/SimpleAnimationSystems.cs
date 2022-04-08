using Unity.Deformations;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateBlendShapeWeightSystem : SystemBase
{
    const float k_Two_Pi = 2f * math.PI;

    protected override void OnUpdate()
    {
        var t = (float)Time.ElapsedTime;

        Entities.ForEach((ref DynamicBuffer<BlendShapeWeight> weights, in AnimateBlendShape data) =>
        {
            var frequency = data.Frequency;
            var phase = data.PhaseShift;
            var interpolation = 0.5f * (math.sin(k_Two_Pi * frequency * (t + phase)) + 1f);
            var weight = math.lerp(data.From, data.To, interpolation);

            for (int i = 0; i < weights.Length; ++i)
            {
                weights[i] = new BlendShapeWeight { Value = weight };
            }
        }).ScheduleParallel();
    }
}

[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimatePositionSystem : SystemBase
{
    const float k_Two_Pi = 2f * math.PI;

    protected override void OnUpdate()
    {
        var t = (float)Time.ElapsedTime;

        Entities.ForEach((ref Translation translation, in AnimatePosition data) =>
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (t + data.PhaseShift)) + 1f);
            translation.Value = math.lerp(data.From, data.To, normalizedTime);
        }).ScheduleParallel();
    }
}

[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateRotationSystem : SystemBase
{
    const float k_Two_Pi = 2f * math.PI;

    protected override void OnUpdate()
    {
        var t = (float)Time.ElapsedTime;

        Entities.ForEach((ref Rotation rotation, in AnimateRotation data) =>
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (t + data.PhaseShift)) + 1f);
            rotation.Value = math.slerp(data.From, data.To, normalizedTime);
        }).ScheduleParallel();
    }
}

[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateScaleSystem : SystemBase
{
    const float k_Two_Pi = 2f * math.PI;

    protected override void OnUpdate()
    {
        var t = (float)Time.ElapsedTime;

        Entities.ForEach((ref NonUniformScale scale, in AnimateScale data) =>
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (t + data.PhaseShift)) + 1f);
            scale.Value = math.lerp(data.From, data.To, normalizedTime);
        }).ScheduleParallel();
    }
}
