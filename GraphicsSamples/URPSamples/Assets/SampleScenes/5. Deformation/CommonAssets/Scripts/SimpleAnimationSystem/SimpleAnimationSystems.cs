using Unity.Deformations;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateBlendShapeWeightSystem : SystemBase
{
    partial struct AnimateBlendShapeWeightJob : IJobEntity
    {
        const float k_Two_Pi = 2f * math.PI;
        public float ElapsedTime;

        void Execute(ref DynamicBuffer<BlendShapeWeight> weights, in AnimateBlendShape data)
        {
            var frequency = data.Frequency;
            var phase = data.PhaseShift;
            var interpolation = 0.5f * (math.sin(k_Two_Pi * frequency * (ElapsedTime + phase)) + 1f);
            var weight = math.lerp(data.From, data.To, interpolation);

            for (int i = 0; i < weights.Length; ++i)
            {
                weights[i] = new BlendShapeWeight { Value = weight };
            }
        }
    }

    protected override void OnUpdate()
    {
        new AnimateBlendShapeWeightJob() { ElapsedTime = (float)SystemAPI.Time.ElapsedTime }.ScheduleParallel();
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimatePositionSystem : SystemBase
{
    partial struct AnimatePositionJob : IJobEntity
    {
        const float k_Two_Pi = 2f * math.PI;
        public float ElapsedTime;

        void Execute(ref LocalTransform localTransform, in AnimatePosition data)
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (ElapsedTime + data.PhaseShift)) + 1f);
            localTransform.Position = math.lerp(data.From, data.To, normalizedTime);
        }
    }

    protected override void OnUpdate()
    {
        new AnimatePositionJob { ElapsedTime = (float)SystemAPI.Time.ElapsedTime }.ScheduleParallel();
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateRotationSystem : SystemBase
{
    partial struct AnimateRotationJob : IJobEntity
    {
        const float k_Two_Pi = 2f * math.PI;
        public float ElapsedTime;

        void Execute(ref LocalTransform localTransform, in AnimateRotation data)
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (ElapsedTime + data.PhaseShift)) + 1f);
            localTransform.Rotation = math.slerp(data.From, data.To, normalizedTime);
        }
    }

    protected override void OnUpdate()
    {
        new AnimateRotationJob { ElapsedTime = (float)SystemAPI.Time.ElapsedTime }.ScheduleParallel();
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
partial class AnimateScaleSystem : SystemBase
{
    partial struct AnimateScaleJob : IJobEntity
    {
        const float k_Two_Pi = 2f * math.PI;
        public float ElapsedTime;

        void Execute(ref PostTransformMatrix matrix, in AnimateScale data)
        {
            var normalizedTime = 0.5f * (math.sin(k_Two_Pi * data.Frequency * (ElapsedTime + data.PhaseShift)) + 1f);
            matrix = new PostTransformMatrix
            {
                Value = float4x4.Scale(math.lerp(data.From, data.To, normalizedTime))
            };
        }
    }

    protected override void OnUpdate()
    {
        new AnimateScaleJob { ElapsedTime = (float)SystemAPI.Time.ElapsedTime }.ScheduleParallel();
    }
}
