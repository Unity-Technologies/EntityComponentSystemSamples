using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids
{
    [RequireMatchingQueriesForUpdate]
    public partial class SampledAnimationClipPlaybackSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = math.min(0.05f, SystemAPI.Time.DeltaTime);

            Entities.ForEach((ref LocalTransform transform, in SampledAnimationClip sampledAnimationClip) =>
            {
                var frameIndex = sampledAnimationClip.FrameIndex;
                var timeOffset = sampledAnimationClip.TimeOffset;

                // Be careful not to cache Value (or any field in Value like Samples) inside of blob asset.
                var prevTranslation = sampledAnimationClip.TransformSamplesBlob.Value.TranslationSamples[frameIndex];
                var nextTranslation = sampledAnimationClip.TransformSamplesBlob.Value.TranslationSamples[frameIndex + 1];
                var prevRotation    = sampledAnimationClip.TransformSamplesBlob.Value.RotationSamples[frameIndex];
                var nextRotation    = sampledAnimationClip.TransformSamplesBlob.Value.RotationSamples[frameIndex + 1];

                transform.Position = math.lerp(prevTranslation, nextTranslation, timeOffset);
                transform.Rotation = math.slerp(prevRotation, nextRotation, timeOffset);
            }).ScheduleParallel();

            Entities.ForEach((ref SampledAnimationClip sampledAnimationClip) =>
            {
                var currentTime = sampledAnimationClip.CurrentTime + deltaTime;
                var sampleRate = sampledAnimationClip.SampleRate;
                var frameIndex = (int)(currentTime / sampledAnimationClip.SampleRate);
                var timeOffset = (currentTime - (frameIndex * sampleRate)) * (1.0f / sampleRate);

                // Just restart loop when over end:
                //   - Don't interpolate between last and first frame.
                //   - Don't worry about interpolating time into the start of the loop.
                //   - Don't worry too much about exactly what the last frame even means.
                if (frameIndex >= (sampledAnimationClip.FrameCount - 2))
                {
                    currentTime = 0.0f;
                    frameIndex = 0;
                    timeOffset = 0.0f;
                }

                sampledAnimationClip.CurrentTime = currentTime;
                sampledAnimationClip.FrameIndex = frameIndex;
                sampledAnimationClip.TimeOffset = timeOffset;
            }).ScheduleParallel();
        }
    }
}
