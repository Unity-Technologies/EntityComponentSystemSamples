using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Baking.BlobAssetBaker
{
    partial struct BlobAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Animation>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            // Apply the animation from the blob asset.
            foreach (var (anim, transform) in
                     SystemAPI.Query<RefRW<Animation>, RefRW<LocalTransform>>())
            {
                anim.ValueRW.T += dt;
                transform.ValueRW.Position.y = Evaluate(anim.ValueRO.T, anim.ValueRO.AnimBlobReference);
            }
        }

        static float Evaluate(float t, BlobAssetReference<AnimationBlobData> anim)
        {
            // normalize t (when t exceeds the curve time, repeat it)
            t *= anim.Value.InvLength;
            t -= math.floor(t);

            // Find index and interpolation value in the array
            float sampleT = t * anim.Value.KeyCount;
            var sampleTFloor = math.floor(sampleT);

            float interp = sampleT - sampleTFloor;
            var index = (int) sampleTFloor;

            return math.lerp(anim.Value.Keys[index], anim.Value.Keys[index + 1], interp);
        }
    }
}
