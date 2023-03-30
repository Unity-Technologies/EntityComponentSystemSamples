using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;

namespace Asteroids.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct StaticAsteroidSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StaticAsteroid>();
        }
        [BurstCompile]
        partial struct StaticAsteroidJob : IJobEntity
        {
            public NetworkTick tick;
            public float tickFraction;
            public float frameTime;

            public void Execute(ref LocalTransform transform, in StaticAsteroid staticAsteroid)
            {
                transform.Position = staticAsteroid.GetPosition(tick, tickFraction, frameTime);
                transform.Rotation = staticAsteroid.GetRotation(tick, tickFraction, frameTime);
            }

        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var asteroidJob = new StaticAsteroidJob
            {
                tick = networkTime.InterpolationTick,
                tickFraction = networkTime.InterpolationTickFraction,
                frameTime = tickRate.SimulationFixedTimeStep
            };
            state.Dependency = asteroidJob.ScheduleParallel(state.Dependency);
        }
    }
}
