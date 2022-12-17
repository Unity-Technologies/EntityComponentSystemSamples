using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.BlobAssets
{
    [BurstCompile]
    partial struct BlobAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            // Apply the animation described in the BlobAsset
            foreach (var (anim, transform) in SystemAPI.Query<RefRW<Animation>, RefRW<LocalTransform>>())
            {

                anim.ValueRW.T += dt;
                transform.ValueRW.Position.y = Evaluate(anim.ValueRO.T, anim.ValueRO.AnimBlobReference);
            }
        }

        static float Evaluate(float t, BlobAssetReference<AnimationBlobData> anim)
        {
            // Loops time value between 0...1
            t = CalculateNormalizedTime(t, anim);

            // Find index and interpolation value in the array
            float sampleT = t * anim.Value.KeyCount;
            var sampleTFloor = math.floor(sampleT);

            float interp = sampleT - sampleTFloor;
            var index = (int) sampleTFloor;

            return math.lerp(anim.Value.Keys[index], anim.Value.Keys[index + 1], interp);
        }

        // When t exceeds the curve time, repeat it
        static float CalculateNormalizedTime(float t, BlobAssetReference<AnimationBlobData> anim)
        {
            float normalizedT = t * anim.Value.InvLength;
            return normalizedT - math.floor(normalizedT);
        }
    }
}