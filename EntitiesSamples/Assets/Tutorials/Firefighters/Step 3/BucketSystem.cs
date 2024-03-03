using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Tutorials.Firefighters
{
    public partial struct BucketSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<ExecuteBucket>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            foreach (var (bucket, trans, color) in
                     SystemAPI.Query<RefRW<Bucket>, RefRW<LocalTransform>, RefRW<URPMaterialPropertyBaseColor>>())
            {
                // todo we only really need to update the color when the water value changes
                color.ValueRW.Value = math.lerp(config.BucketEmptyColor, config.BucketFullColor, bucket.ValueRO.Water);
                trans.ValueRW.Scale = math.lerp(config.BucketEmptyScale, config.BucketFullScale, bucket.ValueRO.Water);

                if (bucket.ValueRO.IsCarried)
                {
                    var botTrans = SystemAPI.GetComponent<LocalTransform>(bucket.ValueRO.CarryingBot);
                    trans.ValueRW.Position = botTrans.Position + new float3(0, 1, 0); // place above the bot's head
                }
            }
        }
    }
}
