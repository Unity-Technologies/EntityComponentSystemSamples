using Unity.Netcode;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Netcode.HostMigration;
using Unity.Netcode.NetcodeTime;
using Unity.Transforms;

namespace Asteroids.Server
{
    /// <summary>Handles spawning Asteroids.</summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    // The system was moved to the InitializationSystemGroup in order to avoid race-conditions with the Netcode systems
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct AsteroidGameSpawnSystem : ISystem
    {
        EntityQuery m_LevelQuery;
        EntityQuery m_ConnectionQuery;
        EntityQuery m_ShipQuery;
        EntityQuery m_DynamicAsteroidsQuery;
        EntityQuery m_StaticAsteroidsQuery;
        EntityQuery m_HostMigrationQuery;

        Entity m_AsteroidPrefab;

        float m_AsteroidRadius;
        float m_ShipRadius;

        private NativeReference<Random> randomReference;
        ComponentLookup<PlayerStateComponentData> playerStateFromEntity;
        ComponentLookup<CommandTarget> commandTargetFromEntity;
        ComponentLookup<NetworkId> networkIdFromEntity;
        ComponentLookup<LocalTransform> localTransformLookup;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, ShipStateComponentData>();

            m_ShipQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<LocalTransform, AsteroidTagComponentData>()
                .WithNone<StaticAsteroid>();

            m_DynamicAsteroidsQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<StaticAsteroid>();

            m_StaticAsteroidsQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAllRW<LevelComponent>();

            m_LevelQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAllRW<NetworkId>(); // can't use NetworkStreamConnection, doesn't work for single world host.

            m_ConnectionQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<HostMigrationInProgress>();

            m_HostMigrationQuery = state.GetEntityQuery(builder);

            state.RequireForUpdate(m_LevelQuery);
            state.RequireForUpdate<AsteroidsSpawner>();

            // Ensure every random is seeded uniquely (not Burst compatible)...
            // AND that the random feedbacks into itself, ensuring better quality randomness for Asteroid spawns.
            var fileTimeUtc = System.DateTime.UtcNow.ToFileTimeUtc();
            randomReference = new NativeReference<Random>(Random.CreateFromIndex((uint) fileTimeUtc), Allocator.Persistent);

            playerStateFromEntity = state.GetComponentLookup<PlayerStateComponentData>();
            commandTargetFromEntity = state.GetComponentLookup<CommandTarget>();
            networkIdFromEntity = state.GetComponentLookup<NetworkId>();
            localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            randomReference.Dispose();
            // Others are disposed automatically.
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (m_ConnectionQuery.IsEmptyIgnoreFilter)
            {
                // No connected players, just destroy all asteroids to save CPU
                state.EntityManager.DestroyEntity(m_StaticAsteroidsQuery);
                state.EntityManager.DestroyEntity(m_DynamicAsteroidsQuery);
                return;
            }

            // If there is a host migration in progress skip any spawning here until it's done
            if (!m_HostMigrationQuery.IsEmptyIgnoreFilter)
                return;

            var settings = SystemAPI.GetSingleton<ServerSettings>();
            if (m_AsteroidPrefab == Entity.Null)
            {
                var asteroidsSpawner = SystemAPI.GetSingleton<AsteroidsSpawner>();
                m_AsteroidPrefab = settings.levelData.staticAsteroidOptimization ? asteroidsSpawner.StaticAsteroid : asteroidsSpawner.Asteroid;

                if (m_AsteroidPrefab == Entity.Null)
                    return;

                m_AsteroidRadius = settings.levelData.asteroidCollisionRadius;
                m_ShipRadius = settings.levelData.shipCollisionRadius;
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            playerStateFromEntity.Update(ref state);
            commandTargetFromEntity.Update(ref state);
            networkIdFromEntity.Update(ref state);
            localTransformLookup.Update(ref state);

            // Optimization: Prevent gathering hundreds of thousands of asteroids, which kills the main thread.
            // Note: This will cause the ship to potentially spawn inside asteroids, but the bigger map size
            // should make this a low repro event.
            // TODO - We could destroy all asteroids chunks within your spawn tile instead?
            int currentAsteroidsCount = m_DynamicAsteroidsQuery.CalculateEntityCountWithoutFiltering() + m_StaticAsteroidsQuery.CalculateEntityCountWithoutFiltering();

            var shipTransforms = m_ShipQuery.ToComponentDataListAsync<LocalTransform>(state.WorldUpdateAllocator, out var shipTranslationsHandle);
            var level = m_LevelQuery.ToComponentDataListAsync<LevelComponent>(state.WorldUpdateAllocator, out var levelHandle);

            state.Dependency = JobHandle.CombineDependencies(shipTranslationsHandle, levelHandle, state.Dependency);

            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();

            var asteroidLevelPadding = m_AsteroidRadius + 3;
            var minShipAsteroidSpawnDistance = m_ShipRadius + m_AsteroidRadius + 100;

            var shipTransformsList = new NativeList<LocalTransform>(64, state.WorldUpdateAllocator);

            var shipListJob = new CreateShipListJob
            {
                shipTransformsIn = shipTransforms,
                shipTransformsOut = shipTransformsList,
            };
            state.Dependency = shipListJob.Schedule(state.Dependency);

            var spawnAsteroids = new SpawnAllAsteroids
            {
                ecb = ecb,
                shipTransforms = shipTransformsList.AsDeferredJobArray(),
                localTransformLookup = localTransformLookup,
                level = level,
                random = randomReference,
                tick = tick,
                asteroidPrefab = m_AsteroidPrefab,
                asteroidLevelPadding = asteroidLevelPadding,
                minShipAsteroidSpawnDistance = minShipAsteroidSpawnDistance,
                currentAsteroidsCount = currentAsteroidsCount,
                numAsteroids = settings.levelData.numAsteroids,
                asteroidVelocity = settings.levelData.asteroidVelocity,
                staticAsteroidOptimization = settings.levelData.staticAsteroidOptimization ? 1: 0
            };
            state.Dependency = spawnAsteroids.Schedule(state.Dependency);
        }

        [BurstCompile]
        struct CreateShipListJob : IJob
        {
            [ReadOnly] public NativeList<LocalTransform> shipTransformsIn;
            public NativeList<LocalTransform> shipTransformsOut;
            public void Execute()
            {
                shipTransformsOut.AddRange(shipTransformsIn.AsArray());
            }
        }

        [BurstCompile]
        struct SpawnAllAsteroids : IJob
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public NativeArray<LocalTransform> shipTransforms;
            [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
            [ReadOnly] public NativeList<LevelComponent> level;

            public NativeReference<Random> random;
            public NetworkTick tick;
            public Entity asteroidPrefab;
            public float asteroidLevelPadding;
            public float minShipAsteroidSpawnDistance;
            public int currentAsteroidsCount;
            public int numAsteroids;
            public float asteroidVelocity;
            public int staticAsteroidOptimization;

            public void Execute()
            {
                var rand = random.Value;
                for (int i = currentAsteroidsCount; i < numAsteroids; ++i)
                {
                    // Spawn asteroid at random pos, assuming we can find a valid one that isn't under a ship.
                    // Don't treat this as an error (because it may happen occasionally by chance, or if the map is packed with ships).
                    // Instead, just stop attempting to spawn any more this frame.
                    if (!TryFindSpawnPos(ref rand, shipTransforms, level[0], asteroidLevelPadding, minShipAsteroidSpawnDistance, out var validAsteroidPos))
                        break;

                    var angle = rand.NextFloat(-0.0f, 359.0f);
                    //@ronald. this is necessary since the meshes are not backing the correct scaling factor
                    var originalScale = localTransformLookup[asteroidPrefab].Scale;
                    var trans = LocalTransform.FromPositionRotationScale(
                        validAsteroidPos,
                        quaternion.RotateZ(math.radians(angle)),
                        originalScale);
                    var vel = new Velocity {Value = math.mul(trans.Rotation, new float3(0, asteroidVelocity, 0)).xy};

                    var e = ecb.Instantiate(asteroidPrefab);

                    ecb.SetComponent(e, trans);
                    if (staticAsteroidOptimization == 1)
                    {
                        UnityEngine.Debug.Assert(tick.IsValid);
                        ecb.SetComponent(e,
                            new StaticAsteroid
                            {
                                InitialPosition = trans.Position.xy, InitialVelocity = vel.Value, InitialAngle = angle,
                                SpawnTick = tick,
                            });
                    }
                    else
                    {
                        ecb.SetComponent(e, vel);
                    }
                }

                random.Value = rand;
            }
        }

        public static bool TryFindSpawnPos(ref Random rand, NativeArray<LocalTransform> avoidTransforms, LevelComponent levelComponent, float levelPadding, float minSpawnDistance, out float3 validRandomAsteroidPosition)
        {
            validRandomAsteroidPosition = 0;
            var minSpawnDistanceSqr = minSpawnDistance * minSpawnDistance;

            for (var attempt = 0; attempt < 5; attempt++)
            {
                validRandomAsteroidPosition = new float3(rand.NextFloat(levelPadding, levelComponent.levelWidth - levelPadding), rand.NextFloat(levelPadding, levelComponent.levelHeight - levelPadding), 0);

                var isValidLocation = true;
                for (var i = 0; i < avoidTransforms.Length; i++)
                {
                    if (math.distancesq(avoidTransforms[i].Position, validRandomAsteroidPosition) < minSpawnDistanceSqr)
                    {
                        isValidLocation = false;
                        break;
                    }
                }
                if(isValidLocation)
                    return true;
                minSpawnDistanceSqr *= 0.8f; // Reduce size & retry.
            }
            return false;
        }
    }
}
