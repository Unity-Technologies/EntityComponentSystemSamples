using Unity.Deformations;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateBlendShapeWeightSystem : SystemBase
{
    const float k_Two_Pi = 2f * math.PI;

    protected override void OnUpdate()
    {
        var t = (float)SystemAPI.Time.ElapsedTime;

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

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimatePositionSystem : SystemBase
{
    const float k_Two_Pi = 2f * math.PI;

    protected override void OnUpdate()
    {
        var t = (float)SystemAPI.Time.ElapsedTime;

        Entities.ForEach((ref LocalTransform localTransform, in AnimatePosition data) =>
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (t + data.PhaseShift)) + 1f);
            localTransform.Position = math.lerp(data.From, data.To, normalizedTime);
        }).ScheduleParallel();
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateRotationSystem : SystemBase
{
    const float k_Two_Pi = 2f * math.PI;

    protected override void OnUpdate()
    {
        var t = (float)SystemAPI.Time.ElapsedTime;

        Entities.ForEach((ref LocalTransform localTransform, in AnimateRotation data) =>
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (t + data.PhaseShift)) + 1f);
            localTransform.Rotation = math.slerp(data.From, data.To, normalizedTime);
        }).ScheduleParallel();
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateScaleSystem : SystemBase
{
    const float k_Two_Pi = 2f * math.PI;

    protected override void OnUpdate()
    {
        var t = (float)SystemAPI.Time.ElapsedTime;

        Entities.ForEach((ref PostTransformMatrix matrix, in AnimateScale data) =>
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (t + data.PhaseShift)) + 1f);
            matrix = new PostTransformMatrix
            {
                Value = float4x4.Scale(math.lerp(data.From, data.To, normalizedTime))
            };
        }).ScheduleParallel();
    }
}
