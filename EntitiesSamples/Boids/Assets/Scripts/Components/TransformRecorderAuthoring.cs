#if UNITY_EDITOR

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

// Demonstrate taking some data available in editor about GameObjects and save in
// a runtime format suitable for Component system updates.
// - Playback first attached animation clip (only expect one)
// - Record positions and rotations at specified rate
// - Store samples into DynamicBuffer

namespace Samples.Boids
{
    public class TransformRecorderAuthoring : MonoBehaviour
    {
        [Range(2, 120)] public int SamplesPerSecond = 60;

        public class TransformRecorderAuthoringBaker : Baker<TransformRecorderAuthoring>
        {
            public override void Bake(TransformRecorderAuthoring authoring)
            {
                var animationClips = AnimationUtility.GetAnimationClips(authoring.gameObject);
                var animationClip = animationClips[0];
                var lengthSeconds = animationClip.length;
                var sampleRate = 1.0f / authoring.SamplesPerSecond;
                var frameCount = (int)(lengthSeconds / sampleRate);
                if (frameCount < 2) // Minimum two frames of animation to capture.
                {
                    return;
                }

                var s = 0.0f;

                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var transformSamplesBlob = ref blobBuilder.ConstructRoot<TransformSamples>();
                var translationSamples = blobBuilder.Allocate(ref transformSamplesBlob.TranslationSamples, frameCount);
                var rotationSamples = blobBuilder.Allocate(ref transformSamplesBlob.RotationSamples, frameCount);

                for (int i = 0; i < frameCount; i++)
                {
                    animationClip.SampleAnimation(authoring.gameObject, s);

                    translationSamples[i] = authoring.gameObject.transform.position;
                    rotationSamples[i] = authoring.gameObject.transform.rotation;

                    s += sampleRate;
                }

                AddComponent(new SampledAnimationClip
                {
                    FrameCount = frameCount,
                    SampleRate = sampleRate,
                    CurrentTime = 0.0f,
                    FrameIndex = 0,
                    TimeOffset = 0,
                    TransformSamplesBlob = blobBuilder.CreateBlobAssetReference<TransformSamples>(Allocator.Persistent)
                });

                blobBuilder.Dispose();
            }
        }
    }
    
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


#endif
