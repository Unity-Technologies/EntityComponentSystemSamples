using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids
{
    public struct SampledAnimationClip : IComponentData
    {
        public float SampleRate;
        public int FrameCount;

        // Playback State
        public float CurrentTime;
        public int FrameIndex;
        public float TimeOffset;

        public BlobAssetReference<TransformSamples> TransformSamplesBlob;
    }

    [WriteGroup(typeof(LocalTransform))]
    public struct TransformSamples
    {
        public BlobArray<float3> TranslationSamples;
        public BlobArray<quaternion> RotationSamples;
    }
}
