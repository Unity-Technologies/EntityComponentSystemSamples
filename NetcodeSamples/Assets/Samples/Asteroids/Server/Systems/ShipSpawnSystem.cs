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
    /// <summary>Handles spawning of Ships.</summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(AsteroidGameSpawnSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ShipSpawnSystem : ISystem
    {
        EntityQuery m_LevelQuery;
        EntityQuery m_ConnectionQuery;
        EntityQuery m_DynamicAsteroidsQuery;
        EntityQuery m_StaticAsteroidsQuery;
        EntityQuery m_HostMigrationQuery;

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
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, AsteroidTagComponentData>()
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
            randomReference =
                new NativeReference<Random>(Random.CreateFromIndex((uint)fileTimeUtc), Allocator.Persistent);

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
            // If there is a host migration in progress skip any spawning here until it's done
            if (!m_HostMigrationQuery.IsEmptyIgnoreFilter)
                return;

            var settings = SystemAPI.GetSingleton<ServerSettings>();
            if (m_ShipPrefab == Entity.Null)
            {
                var asteroidsSpawner = SystemAPI.GetSingleton<AsteroidsSpawner>();
                m_ShipPrefab = asteroidsSpawner.Ship;

                if (m_ShipPrefab == Entity.Null)
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
            int currentAsteroidsCount = m_DynamicAsteroidsQuery.CalculateEntityCountWithoutFiltering() +
                                        m_StaticAsteroidsQuery.CalculateEntityCountWithoutFiltering();
            NativeList<Entity> dynamicAsteroidEntities = default;
            NativeList<LocalTransform> dynamicAsteroidTransforms = default;
            NativeList<StaticAsteroid> staticAsteroids = default;
            NativeList<Entity> staticAsteroidEntities = default;
            var tooManyEntitiesToGather = currentAsteroidsCount > 10_000;
            if (tooManyEntitiesToGather)
            {
                dynamicAsteroidEntities = new(0, state.WorldUpdateAllocator);
                dynamicAsteroidTransforms = new(0, state.WorldUpdateAllocator);
                staticAsteroids = new(0, state.WorldUpdateAllocator);
                staticAsteroidEntities = dynamicAsteroidEntities;
            }
            else
            {
                dynamicAsteroidEntities =
                    m_DynamicAsteroidsQuery.ToEntityListAsync(state.WorldUpdateAllocator,
                        out var asteroidEntitiesHandle);
                dynamicAsteroidTransforms =
                    m_DynamicAsteroidsQuery.ToComponentDataListAsync<LocalTransform>(state.WorldUpdateAllocator,
                        out var asteroidTranslationsHandle);
                staticAsteroids =
                    m_StaticAsteroidsQuery.ToComponentDataListAsync<StaticAsteroid>(state.WorldUpdateAllocator,
                        out var staticAsteroidsHandle);
                staticAsteroidEntities = m_StaticAsteroidsQuery.ToEntityListAsync(state.WorldUpdateAllocator,
                    out var staticAsteroidEntitiesHandle);
                state.Dependency = JobHandle.CombineDependencies(asteroidEntitiesHandle, asteroidTranslationsHandle,
                    JobHandle.CombineDependencies(staticAsteroidsHandle, staticAsteroidEntitiesHandle,
                        state.Dependency));
            }

            var level = m_LevelQuery.ToComponentDataListAsync<LevelComponent>(state.WorldUpdateAllocator,
                out var levelHandle);

            state.Dependency = JobHandle.CombineDependencies(levelHandle, state.Dependency);

            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();
            var fixedDeltaTime = tickRate.SimulationFixedTimeStep;

            var shipLevelPadding = m_ShipRadius + 50;
            var minShipAsteroidSpawnDistance = m_ShipRadius + m_AsteroidRadius + 100;
            var minShipToShipSpawnDistance = (m_ShipRadius + m_ShipRadius) + 300;

            var shipTransformsList = new NativeList<LocalTransform>(64, state.WorldUpdateAllocator);

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
                AsteroidGameSpawnSystem.TryFindSpawnPos(ref rand, shipTransforms.AsArray(), level[0], shipLevelPadding,
                    minShipToShipSpawnDistance, out var validShipPos);
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
                ecb.SetComponent(shipEntity,
                    new GhostOwner { NetworkId = networkIdFromEntity[requestSource.SourceConnection].Value });
                ecb.SetComponent(shipEntity,
                    new PlayerIdComponentData { PlayerEntity = requestSource.SourceConnection });
                ecb.SetComponent(requestSource.SourceConnection, new CommandTarget { targetEntity = shipEntity });
                ecb.SetComponent(requestSource.SourceConnection, new PlayerStateComponentData { IsSpawning = 0 });
                ecb.AppendToBuffer(requestSource.SourceConnection, new LinkedEntityGroup { Value = shipEntity });

                // Add to the list to prevent asteroids below from spawning near them.
                shipTransforms.Add(trans);

                // Mark the player as currently spawning
                playerStateFromEntity[requestSource.SourceConnection] = new PlayerStateComponentData { IsSpawning = 1 };

                // Destroy asteroids that are too close to this spawn:
                var minShipAsteroidSpawnDistanceSqr = minShipAsteroidSpawnDistance * minShipAsteroidSpawnDistance;
                for (int i = 0; i < dynamicAsteroidTransforms.Length; i++)
                {
                    if (math.distancesq(dynamicAsteroidTransforms[i].Position, validShipPos) <
                        minShipAsteroidSpawnDistanceSqr)
                        ecb.DestroyEntity(dynamicAsteroidEntities[i]);
                }

                for (int i = 0; i < staticAsteroids.Length; i++)
                {
                    if (math.distancesq(staticAsteroids[i].GetPosition(tick, 1f, fixedDeltaTime), validShipPos) <
                        minShipAsteroidSpawnDistanceSqr)
                        ecb.DestroyEntity(staticAsteroidEntities[i]);
                }
            }
        }
    }
}
