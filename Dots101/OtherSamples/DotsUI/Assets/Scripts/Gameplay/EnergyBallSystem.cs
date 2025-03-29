using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.DotsUISample
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct EnergyBallSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var player = SystemAPI.GetSingletonRW<Player>();
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var playerPosition = SystemAPI.GetComponentRO<LocalTransform>(playerEntity).ValueRO.Position;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, energy) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<Energy>>())
            {
                // If the energy ball is already collected, have it fly in a circle around the player
                if (energy.ValueRO.Collected)
                {
                    var indexSeparation = energy.ValueRO.Index * math.PI / 2;
                    transform.ValueRW.Position = playerPosition + new float3(
                        (float)(math.cos(SystemAPI.Time.ElapsedTime * 5f + indexSeparation) * 1.5f),
                        1.5f,
                        (float)(math.sin(SystemAPI.Time.ElapsedTime * 5f + indexSeparation) * 1.5f));

                    continue;
                }

                float distance = math.distance(transform.ValueRO.Position.xz, playerPosition.xz);

                // If the player is far from the energy ball, move it up and down
                if (distance > 4f)
                {
                    transform.ValueRW.Position.y = (float)(math.sin(SystemAPI.Time.ElapsedTime) * 0.5f + 2f);
                }
                else if (distance < 5f)
                {
                    // If the player is close to the energy ball, move it towards the player
                    var t = SystemAPI.Time.DeltaTime * 5f;
                    transform.ValueRW.Position.xz = math.lerp(transform.ValueRO.Position.xz, playerPosition.xz, t);
                    transform.ValueRW.Scale = math.lerp(transform.ValueRO.Scale, 0.5f, t);

                    // If the player is close enough, collect the energy ball
                    if (distance < 1f)
                    {
                        energy.ValueRW.Collected = true;
                        transform.ValueRW.Scale = 0.5f;

                        player.ValueRW.EnergyCount++;
                        var eventEntity = ecb.CreateEntity();
                        ecb.AddComponent<Event>(eventEntity);
                        ecb.AddComponent<PickupEvent>(eventEntity);
                    }
                }
            }
        }
    }
}