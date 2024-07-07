using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Asteroids.Server
{
    /// <summary>Handles spawning of Ships and Asteroids.</summary>
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

        Entity m_AsteroidPrefab;
        Entity m_ShipPrefab;

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
            builder.WithAllRW<NetworkStreamConnection>();

            m_ConnectionQuery = state.GetEntityQuery(builder);

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

            var settings = SystemAPI.GetSingleton<ServerSettings>();
            if (m_AsteroidPrefab == Entity.Null || m_ShipPrefab == Entity.Null)
            {
                var asteroidsSpawner = SystemAPI.GetSingleton<AsteroidsSpawner>();
                m_AsteroidPrefab = settings.levelData.staticAsteroidOptimization ? asteroidsSpawner.StaticAsteroid : asteroidsSpawner.Asteroid;
                m_ShipPrefab = asteroidsSpawner.Ship;

                if (m_AsteroidPrefab == Entity.Null || m_ShipPrefab == Entity.Null)
                    return;

                m_AsteroidRadius = state.EntityManager.GetComponentData<CollisionSphereComponent>(m_AsteroidPrefab).radius;
                m_ShipRadius = state.EntityManager.GetComponentData<CollisionSphereComponent>(m_ShipPrefab).radius;
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            playerStateFromEntity.Update(ref state);
            commandTargetFromEntity.Update(ref state);
            networkIdFromEntity.Update(ref state);
            localTransformLookup.Update(ref state);

            var dynamicAsteroidEntities = m_DynamicAsteroidsQuery.ToEntityListAsync(state.WorldUpdateAllocator,
                out var asteroidEntitiesHandle);
            var dynamicAsteroidTransforms = m_DynamicAsteroidsQuery.ToComponentDataListAsync<LocalTransform>(state.WorldUpdateAllocator,
                out var asteroidTranslationsHandle);

            var staticAsteroids = m_StaticAsteroidsQuery.ToComponentDataListAsync<StaticAsteroid>(state.WorldUpdateAllocator,
                out var staticAsteroidsHandle);
            var staticAsteroidEntities = m_StaticAsteroidsQuery.ToEntityListAsync(state.WorldUpdateAllocator,
                out var staticAsteroidEntitiesHandle);
            var shipTransforms = m_ShipQuery.ToComponentDataListAsync<LocalTransform>(state.WorldUpdateAllocator,
                out var shipTranslationsHandle);

            var level = m_LevelQuery.ToComponentDataListAsync<LevelComponent>(state.WorldUpdateAllocator,
                out var levelHandle);

            var jobHandleIt = 0;
            var jobHandles = new NativeArray<JobHandle>(6, Allocator.Temp);
            jobHandles[jobHandleIt++] = asteroidEntitiesHandle;
            jobHandles[jobHandleIt++] = asteroidTranslationsHandle;
            jobHandles[jobHandleIt++] = staticAsteroidsHandle;
            jobHandles[jobHandleIt++] = staticAsteroidEntitiesHandle;
            jobHandles[jobHandleIt++] = shipTranslationsHandle;
            jobHandles[jobHandleIt] = levelHandle;

            var combinedDependencies = JobHandle.CombineDependencies(jobHandles);
            jobHandles.Dispose();

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, combinedDependencies);

            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();
            var fixedDeltaTime = 1.0f / (float) tickRate.SimulationTickRate;

            var shipLevelPadding = m_ShipRadius + 50;
            var asteroidLevelPadding = m_AsteroidRadius + 3;
            var minShipAsteroidSpawnDistance = m_ShipRadius + m_AsteroidRadius + 100;
            var minShipToShipSpawnDistance = (m_ShipRadius + m_ShipRadius) + 300;

            var shipTransformsList = new NativeList<LocalTransform>(64, state.WorldUpdateAllocator);

            var shipListJob = new CreateShipListJob
            {
                shipTransformsIn = shipTransforms,
                shipTransformsOut = shipTransformsList,
            };
            state.Dependency = shipListJob.Schedule(state.Dependency);

            var spawnPlayerShips = new SpawnPlayerShips
            {
                ecb = ecb,
                playerStateFromEntity = playerStateFromEntity,
                commandTargetFromEntity = commandTargetFromEntity,
                networkIdFromEntity = networkIdFromEntity,
                shipTransforms = shipTransformsList,
                dynamicAsteroidTransforms = dynamicAsteroidTransforms,
                localTransformLookup = localTransformLookup,
                staticAsteroids = staticAsteroids,
                dynamicAsteroidEntities = dynamicAsteroidEntities,
                staticAsteroidEntities = staticAsteroidEntities,
                level = level,
                random = randomReference,
                tick = tick,
                shipPrefab = m_ShipPrefab,
                fixedDeltaTime = fixedDeltaTime,
                shipLevelPadding = shipLevelPadding,
                minShipAsteroidSpawnDistance = minShipAsteroidSpawnDistance,
                minShipToShipSpawnDistance = minShipToShipSpawnDistance
            };
            state.Dependency = spawnPlayerShips.Schedule(state.Dependency);

            var spawnAsteroids = new SpawnAllAsteroids
            {
                ecb = ecb,
                dynamicAsteroidEntities = dynamicAsteroidEntities,
                staticAsteroidEntities = staticAsteroidEntities,
                shipTransformsList = shipTransformsList,
                localTransformLookup = localTransformLookup,
                level = level,
                random = randomReference,
                tick = tick,
                asteroidPrefab = m_AsteroidPrefab,
                asteroidLevelPadding = asteroidLevelPadding,
                minShipAsteroidSpawnDistance = minShipAsteroidSpawnDistance,
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
        [WithAll(typeof(PlayerSpawnRequest))]
        internal partial struct SpawnPlayerShips : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public ComponentLookup<PlayerStateComponentData> playerStateFromEntity;
            public ComponentLookup<CommandTarget> commandTargetFromEntity;
            public ComponentLookup<NetworkId> networkIdFromEntity;
            public NativeList<LocalTransform> shipTransforms;
            [ReadOnly] public NativeList<LocalTransform> dynamicAsteroidTransforms;
            [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
            [ReadOnly] public NativeList<StaticAsteroid> staticAsteroids;
            [ReadOnly] public NativeList<Entity> dynamicAsteroidEntities;
            [ReadOnly] public NativeList<Entity> staticAsteroidEntities;
            [ReadOnly] public NativeList<LevelComponent> level;

            public NativeReference<Random> random;
            public NetworkTick tick;
            public Entity shipPrefab;
            public float fixedDeltaTime;
            public float shipLevelPadding;
            public float minShipAsteroidSpawnDistance;
            public float minShipToShipSpawnDistance;

            void Execute(Entity entity, in ReceiveRpcCommandRequest requestSource)
            {
                // Destroy the spawn request:
                ecb.DestroyEntity(entity);

                // Is request even valid?
                if (!playerStateFromEntity.HasComponent(requestSource.SourceConnection) ||
                    !commandTargetFromEntity.HasComponent(requestSource.SourceConnection) ||
                    commandTargetFromEntity[requestSource.SourceConnection].targetEntity != Entity.Null ||
                    playerStateFromEntity[requestSource.SourceConnection].IsSpawning != 0)
                    return;

                // Try find a random spawn position for the Ship that isn't near another player.
                // Don't allow failure though, just take a "bad" position instead.
                var rand = random.Value;
                TryFindSpawnPos(ref rand, shipTransforms.AsArray(), level[0], shipLevelPadding, minShipToShipSpawnDistance, out var validShipPos);
                random.Value = rand;

                // Instantiate ship:
                var shipEntity = ecb.Instantiate(shipPrefab);
                //@ronald. this is necessary since the meshes are not backing the correct scaling factor
                var originalScale = localTransformLookup[shipPrefab].Scale;
                var trans = LocalTransform.FromPositionRotationScale(
                        validShipPos,
                        quaternion.RotateZ(math.radians(90f)),
                        originalScale
                    );

                ecb.SetComponent(shipEntity, trans);
                ecb.SetComponent(shipEntity, new GhostOwner {NetworkId = networkIdFromEntity[requestSource.SourceConnection].Value});
                ecb.SetComponent(shipEntity, new PlayerIdComponentData {PlayerEntity = requestSource.SourceConnection});
                ecb.SetComponent(requestSource.SourceConnection, new CommandTarget {targetEntity = shipEntity});
                ecb.SetComponent(requestSource.SourceConnection, new PlayerStateComponentData {IsSpawning = 0});
                ecb.AppendToBuffer(requestSource.SourceConnection, new LinkedEntityGroup {Value = shipEntity});

                // Add to the list to prevent asteroids below from spawning near them.
                shipTransforms.Add(trans);

                // Mark the player as currently spawning
                playerStateFromEntity[requestSource.SourceConnection] = new PlayerStateComponentData {IsSpawning = 1};

                // Destroy asteroids that are too close to this spawn:
                var minShipAsteroidSpawnDistanceSqr = minShipAsteroidSpawnDistance * minShipAsteroidSpawnDistance;
                for (int i = 0; i < dynamicAsteroidTransforms.Length; i++)
                {
                    if (math.distancesq(dynamicAsteroidTransforms[i].Position, validShipPos) < minShipAsteroidSpawnDistanceSqr)
                        ecb.DestroyEntity(dynamicAsteroidEntities[i]);
                }
                for (int i = 0; i < staticAsteroids.Length; i++)
                {
                    if (math.distancesq(staticAsteroids[i].GetPosition(tick, 1f, fixedDeltaTime), validShipPos) < minShipAsteroidSpawnDistanceSqr)
                        ecb.DestroyEntity(staticAsteroidEntities[i]);
                }
            }
        }

        [BurstCompile]
        struct SpawnAllAsteroids : IJob
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public NativeList<Entity> dynamicAsteroidEntities;
            [ReadOnly] public NativeList<Entity> staticAsteroidEntities;
            [ReadOnly] public NativeList<LocalTransform> shipTransformsList;
            [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
            [ReadOnly] public NativeList<LevelComponent> level;

            public NativeReference<Random> random;
            public NetworkTick tick;
            public Entity asteroidPrefab;
            public float asteroidLevelPadding;
            public float minShipAsteroidSpawnDistance;
            public int numAsteroids;
            public float asteroidVelocity;
            public int staticAsteroidOptimization;

            public void Execute()
            {
                var currentNumAsteroids = staticAsteroidEntities.Length + dynamicAsteroidEntities.Length;
                var rand = random.Value;
                for (int i = currentNumAsteroids; i < numAsteroids; ++i)
                {
                    // Spawn asteroid at random pos, assuming we can find a valid one that isn't under a ship.
                    // Don't treat this as an error (because it may happen occasionally by chance, or if the map is packed with ships).
                    // Instead, just stop attempting to spawn any more this frame.
                    if (!TryFindSpawnPos(ref rand, shipTransformsList.AsArray(), level[0], asteroidLevelPadding, minShipAsteroidSpawnDistance, out var validAsteroidPos))
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
                        ecb.SetComponent(e,
                            new StaticAsteroid
                            {
                                InitialPosition = trans.Position.xy, InitialVelocity = vel.Value, InitialAngle = angle,
                                SpawnTick = tick
                            });
                    else
                        ecb.SetComponent(e, vel);
                }

                random.Value = rand;
            }
        }

        static bool TryFindSpawnPos(ref Random rand, NativeArray<LocalTransform> avoidTransforms, LevelComponent levelComponent, float levelPadding, float minSpawnDistance, out float3 validRandomAsteroidPosition)
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
            }
            return false;
        }
    }
}
