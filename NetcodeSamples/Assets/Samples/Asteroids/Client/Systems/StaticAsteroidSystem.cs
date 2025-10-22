using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;

namespace Asteroids.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct StaticAsteroidSystem : ISystem
    {
        private EntityQuery m_StaticAsteroidsQuery;
        private ComponentTypeHandle<LocalTransform> m_LocalTransforms;
        private ComponentTypeHandle<StaticAsteroid> m_StaticAsteroids;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StaticAsteroid>();
            state.RequireForUpdate<NetworkId>();
            m_StaticAsteroidsQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, StaticAsteroid>());
            m_LocalTransforms = state.GetComponentTypeHandle<LocalTransform>(isReadOnly:false);
            m_StaticAsteroids = state.GetComponentTypeHandle<StaticAsteroid>(isReadOnly:true);
        }
        [BurstCompile]
        partial struct StaticAsteroidJob : IJobChunk
        {
            public ComponentTypeHandle<LocalTransform> localTransformsHandle;
            [ReadOnly] public ComponentTypeHandle<StaticAsteroid> staticAsteroidsHandle;
            public NetworkTick tick;
            public float tickFraction;
            public uint simulationStepBatchSize;
            public float frameTime;
            public uint roundRobinFrequency;
            public bool isServer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // We update the server values infrequently, as we only need the updated
                // LocalTransform positions for distance importance scaling & relevancy calculations.
                if (isServer && (chunk.SequenceNumber + tick.TickIndexForValidTick) % roundRobinFrequency >= simulationStepBatchSize) return;

                var localTransforms = chunk.GetNativeArray(ref localTransformsHandle).AsSpan();
                var staticAsteroids = chunk.GetNativeArray(ref staticAsteroidsHandle).AsReadOnlySpan();
                var entityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (entityEnumerator.NextEntityIndex(out var i))
                {
                    ref var localTransform = ref localTransforms[i];
                    ref readonly var staticAsteroid = ref staticAsteroids[i];
                    localTransform.Position = staticAsteroid.GetPosition(tick, tickFraction, frameTime);
                    localTransform.Rotation = staticAsteroid.GetRotation(tick, tickFraction, frameTime);
                }
            }
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.InterpolationTick.IsValid) return;
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();
            m_LocalTransforms.Update(ref state);
            m_StaticAsteroids.Update(ref state);
            var asteroidJob = new StaticAsteroidJob
            {
                localTransformsHandle = m_LocalTransforms,
                staticAsteroidsHandle = m_StaticAsteroids,
                tick = networkTime.InterpolationTick,
                tickFraction = networkTime.InterpolationTickFraction,
                simulationStepBatchSize = (uint) networkTime.SimulationStepBatchSize,
                roundRobinFrequency = (uint) (tickRate.SimulationTickRate * 30), // Seconds.
                frameTime = tickRate.SimulationFixedTimeStep,
                isServer = state.WorldUnmanaged.IsServer(),
            };
            state.Dependency = asteroidJob.ScheduleParallel(m_StaticAsteroidsQuery, state.Dependency);
            JobHandle.ScheduleBatchedJobs();
        }
    }
}
