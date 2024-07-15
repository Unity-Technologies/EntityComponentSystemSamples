using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Transforms;
using Unity.NetCode;

namespace Asteroids.Server
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(BulletAgeSystem))]
    public partial struct CollisionSystem : ISystem
    {
        private EntityQuery shipQuery;
        private EntityQuery bulletQuery;
        private EntityQuery asteroidQuery;
        private EntityQuery m_LevelQuery;

        private NativeQueue<Entity> playerClearQueue;
        private EntityQuery settingsQuery;

        ComponentTypeHandle<BulletAgeComponent> bulletAgeType;

        ComponentTypeHandle<LocalTransform> transformType;

        ComponentTypeHandle<GhostOwner> ghostOwnerType;
        ComponentTypeHandle<StaticAsteroid> staticAsteroidType;
        ComponentTypeHandle<CollisionSphereComponent> sphereType;
        ComponentTypeHandle<PlayerIdComponentData> playerIdType;
        EntityTypeHandle entityType;

        ComponentLookup<CommandTarget> commandTarget;
        BufferLookup<LinkedEntityGroup> linkedEntityGroupFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)

                .WithAll<LocalTransform, CollisionSphereComponent, ShipTagComponentData, GhostOwner>();

            shipQuery = state.GetEntityQuery(builder);

            builder.Reset();

            builder.WithAll<LocalTransform, CollisionSphereComponent, BulletTagComponent, BulletAgeComponent, GhostOwner>();

            bulletQuery = state.GetEntityQuery(builder);

            builder.Reset();

            builder.WithAll<LocalTransform, CollisionSphereComponent, AsteroidTagComponentData>();

            asteroidQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<ServerSettings>();
            settingsQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<LevelComponent>();
            m_LevelQuery = state.GetEntityQuery(builder);

            playerClearQueue = new NativeQueue<Entity>(Allocator.Persistent);
            state.RequireForUpdate(m_LevelQuery);

            bulletAgeType = state.GetComponentTypeHandle<BulletAgeComponent>(true);

            transformType = state.GetComponentTypeHandle<LocalTransform>(true);

            ghostOwnerType = state.GetComponentTypeHandle<GhostOwner>(true);
            staticAsteroidType = state.GetComponentTypeHandle<StaticAsteroid>(true);
            sphereType = state.GetComponentTypeHandle<CollisionSphereComponent>(true);
            playerIdType = state.GetComponentTypeHandle<PlayerIdComponentData>(true);
            entityType = state.GetEntityTypeHandle();

            commandTarget = state.GetComponentLookup<CommandTarget>();
            linkedEntityGroupFromEntity = state.GetBufferLookup<LinkedEntityGroup>();

            state.RequireForUpdate<AsteroidScore>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            playerClearQueue.Dispose();
        }

        [BurstCompile]
        internal struct DestroyAsteroidJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public NativeList<ArchetypeChunk> bulletChunks;
            [ReadOnly] public ComponentTypeHandle<BulletAgeComponent> bulletAgeType;

            [ReadOnly] public ComponentTypeHandle<LocalTransform> transformType;

            [ReadOnly] public ComponentTypeHandle<StaticAsteroid> staticAsteroidType;
            [ReadOnly] public ComponentTypeHandle<CollisionSphereComponent> sphereType;
            [ReadOnly] public EntityTypeHandle entityType;

            [ReadOnly] public NativeList<LevelComponent> level;
            [NativeSetThreadIndex] public int ThreadIndex;
            [NativeDisableParallelForRestriction] public NativeArray<int> asteroidDestructCounter;
            public NetworkTick tick;
            public float fixedDeltaTime;
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var asteroidSphere = chunk.GetNativeArray(ref sphereType);
                var asteroidEntity = chunk.GetNativeArray(entityType);
                var staticAsteroid = chunk.GetNativeArray(ref staticAsteroidType);
                if (staticAsteroid.IsCreated)
                {
                    for (int asteroid = 0; asteroid < asteroidEntity.Length; ++asteroid)
                    {
                        var firstPos = staticAsteroid[asteroid].GetPosition(tick, 1, fixedDeltaTime).xy;
                        var firstRadius = asteroidSphere[asteroid].radius;
                        CheckCollisions(unfilteredChunkIndex, asteroidEntity[asteroid], firstPos, firstRadius);
                    }
                }
                else
                {

                    var asteroidPos = chunk.GetNativeArray(ref transformType);
                    for (int asteroid = 0; asteroid < asteroidPos.Length; ++asteroid)
                    {
                        var firstPos = asteroidPos[asteroid].Position.xy;

                        var firstRadius = asteroidSphere[asteroid].radius;
                        CheckCollisions(unfilteredChunkIndex, asteroidEntity[asteroid], firstPos, firstRadius);
                    }
                }
            }
            private void CheckCollisions(int unfilteredChunkIndex, Entity asteroidEntity, float2 firstPos, float firstRadius)
            {
                if (firstPos.x - firstRadius < 0 || firstPos.y - firstRadius < 0 ||
                    firstPos.x + firstRadius > level[0].levelHeight ||
                    firstPos.y + firstRadius > level[0].levelHeight)
                {
                    commandBuffer.DestroyEntity(unfilteredChunkIndex, asteroidEntity);
                    return;
                }
                // TODO: can check asteroid / asteroid here if required
                for (int bc = 0; bc < bulletChunks.Length; ++bc)
                {
                    var bulletEntities = bulletChunks[bc].GetNativeArray(entityType);
                    var bulletAge = bulletChunks[bc].GetNativeArray(ref bulletAgeType);

                    var bulletTrans = bulletChunks[bc].GetNativeArray(ref transformType);

                    var bulletSphere = bulletChunks[bc].GetNativeArray(ref sphereType);
                    for (int bullet = 0; bullet < bulletAge.Length; ++bullet)
                    {
                        if (bulletAge[bullet].age > bulletAge[bullet].maxAge)
                            return;

                        var secondPos = bulletTrans[bullet].Position.xy;

                        var secondRadius = bulletSphere[bullet].radius;
                        if (Intersect(firstRadius, secondRadius, firstPos, secondPos))
                        {
                            commandBuffer.DestroyEntity(unfilteredChunkIndex, asteroidEntity);
                            asteroidDestructCounter[ThreadIndex]++;

                            if(level[0].bulletsDestroyedOnContact)
                                commandBuffer.DestroyEntity(unfilteredChunkIndex, bulletEntities[bullet]);
                        }
                    }
                }
            }
        }
        [BurstCompile]
        internal struct DestroyShipJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public NativeList<ArchetypeChunk> asteroidChunks;
            [ReadOnly] public NativeList<ArchetypeChunk> bulletChunks;
            [ReadOnly] public ComponentTypeHandle<BulletAgeComponent> bulletAgeType;

            [ReadOnly] public ComponentTypeHandle<LocalTransform> transformType;

            [ReadOnly] public ComponentTypeHandle<GhostOwner> ghostOwnerType;
            [ReadOnly] public ComponentTypeHandle<StaticAsteroid> staticAsteroidType;
            [ReadOnly] public ComponentTypeHandle<CollisionSphereComponent> sphereType;
            [ReadOnly] public ComponentTypeHandle<PlayerIdComponentData> playerIdType;
            [ReadOnly] public EntityTypeHandle entityType;

            [ReadOnly] public NativeList<ServerSettings> serverSettings;

            public NativeQueue<Entity>.ParallelWriter playerClearQueue;

            [ReadOnly] public NativeList<LevelComponent> level;
            public NetworkTick tick;
            public float fixedDeltaTime;
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);


                var shipTrans = chunk.GetNativeArray(ref transformType);

                var shipSphere = chunk.GetNativeArray(ref sphereType);
                var shipPlayerId = chunk.GetNativeArray(ref playerIdType);
                var shipEntity = chunk.GetNativeArray(entityType);
                var shipGhostOwner = chunk.GetNativeArray(ref ghostOwnerType);


                for (int ship = 0; ship < shipTrans.Length; ++ship)
                {
                    int alive = 1;
                    var firstPos = shipTrans[ship].Position.xy;

                    var firstRadius = shipSphere[ship].radius;
                    if (firstPos.x - firstRadius < 0 || firstPos.y - firstRadius < 0 ||
                        firstPos.x + firstRadius > level[0].levelHeight ||
                        firstPos.y + firstRadius > level[0].levelHeight)
                    {
                        if (shipPlayerId.IsCreated)
                            playerClearQueue.Enqueue(shipPlayerId[ship].PlayerEntity);
                        commandBuffer.DestroyEntity(unfilteredChunkIndex, shipEntity[ship]);
                        continue;
                    }

                    if (serverSettings.Length > 0 && serverSettings[0].levelData.shipPvP)
                    {
                        var shipNetworkId = shipGhostOwner[ship].NetworkId;
                        for (int bc = 0; bc < bulletChunks.Length && alive != 0; ++bc)
                        {
                            var bulletEntities = bulletChunks[bc].GetNativeArray(entityType);
                            var bulletAge = bulletChunks[bc].GetNativeArray(ref bulletAgeType);

                            var bulletPos = bulletChunks[bc].GetNativeArray(ref transformType);

                            var bulletGhostOwner = bulletChunks[bc].GetNativeArray(ref ghostOwnerType);
                            var bulletSphere = bulletChunks[bc].GetNativeArray(ref sphereType);
                            for (int bullet = 0; bullet < bulletAge.Length; ++bullet)
                            {
                                if (bulletAge[bullet].age > bulletAge[bullet].maxAge || bulletGhostOwner[bullet].NetworkId == shipNetworkId)
                                    continue;

                                var secondPos = bulletPos[bullet].Position.xy;

                                var secondRadius = bulletSphere[bullet].radius;
                                if (Intersect(firstRadius, secondRadius, firstPos, secondPos))
                                {
                                    if (shipPlayerId.IsCreated)
                                        playerClearQueue.Enqueue(shipPlayerId[ship].PlayerEntity);
                                    commandBuffer.DestroyEntity(unfilteredChunkIndex, shipEntity[ship]);

                                    if(serverSettings[0].levelData.bulletsDestroyedOnContact)
                                        commandBuffer.DestroyEntity(unfilteredChunkIndex, bulletEntities[bullet]);
                                    alive = 0;
                                    break;
                                }
                            }
                        }
                    }

                    if (alive != 0 && serverSettings.Length > 0 && serverSettings[0].levelData.asteroidsDamageShips)
                    {
                        for (int ac = 0; ac < asteroidChunks.Length && alive != 0; ++ac)
                        {
                            var asteroidSphere = asteroidChunks[ac].GetNativeArray(ref sphereType);
                            var asteroidEntity = asteroidChunks[ac].GetNativeArray(entityType);
                            var staticAsteroid = asteroidChunks[ac].GetNativeArray(ref staticAsteroidType);
                            if (staticAsteroid.IsCreated)
                            {
                                for (int asteroid = 0; asteroid < staticAsteroid.Length; ++asteroid)
                                {
                                    var secondPos = staticAsteroid[asteroid].GetPosition(tick, 1, fixedDeltaTime).xy;
                                    var secondRadius = asteroidSphere[asteroid].radius;
                                    if (Intersect(firstRadius, secondRadius, firstPos, secondPos))
                                    {
                                        if (shipPlayerId.IsCreated)
                                            playerClearQueue.Enqueue(shipPlayerId[ship].PlayerEntity);
                                        commandBuffer.DestroyEntity(unfilteredChunkIndex, shipEntity[ship]);
                                        if(serverSettings[0].levelData.asteroidsDestroyedOnShipContact)
                                            commandBuffer.DestroyEntity(unfilteredChunkIndex, asteroidEntity[asteroid]);
                                        alive = 0;
                                        break;
                                    }
                                }
                            }
                            else
                            {

                                var asteroidTrans = asteroidChunks[ac].GetNativeArray(ref transformType);
                                for (int asteroid = 0; asteroid < asteroidTrans.Length; ++asteroid)
                                {
                                    var secondPos = asteroidTrans[asteroid].Position.xy;

                                    var secondRadius = asteroidSphere[asteroid].radius;
                                    if (Intersect(firstRadius, secondRadius, firstPos, secondPos))
                                    {
                                        if (shipPlayerId.IsCreated)
                                            playerClearQueue.Enqueue(shipPlayerId[ship].PlayerEntity);
                                        commandBuffer.DestroyEntity(unfilteredChunkIndex, shipEntity[ship]);
                                        if(serverSettings[0].levelData.asteroidsDestroyedOnShipContact)
                                            commandBuffer.DestroyEntity(unfilteredChunkIndex, asteroidEntity[asteroid]);
                                        alive = 0;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        internal struct ClearShipPointerJob : IJob
        {
            public NativeQueue<Entity> playerClearQueue;
            public ComponentLookup<CommandTarget> commandTarget;
            public BufferLookup<LinkedEntityGroup> linkedEntityGroupFromEntity;

            public void Execute()
            {
                Entity ent;
                while (playerClearQueue.TryDequeue(out ent))
                {
                    if (commandTarget.HasComponent(ent))
                    {
                        var state = commandTarget[ent];
                        state.targetEntity = Entity.Null;
                        commandTarget[ent] = state;
                        var linkedEntityGroup = linkedEntityGroupFromEntity[ent];
                        linkedEntityGroup.RemoveAt(1);
                    }
                }
            }
        }

        [BurstCompile]
        internal struct GatherAsteroidDestructCounter : IJob
        {
            [ReadOnly] public NativeArray<int> asteroidDestructCounter;
            public EntityCommandBuffer commandBuffer;
            [ReadOnly] public int currentScore;
            [ReadOnly] public Entity scoreSingleton;

            public void Execute()
            {
                int total = currentScore;
                for (int i = 1; i < asteroidDestructCounter.Length; ++i)
                {
                    total += asteroidDestructCounter[i];
                }

                commandBuffer.SetComponent(scoreSingleton, new AsteroidScore { Value = total });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            JobHandle bulletHandle;
            JobHandle asteroidHandle;
            JobHandle levelHandle;
            JobHandle settingsHandle;
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();

            var level = m_LevelQuery.ToComponentDataListAsync<LevelComponent>(state.WorldUpdateAllocator,
                out levelHandle);

            bulletAgeType.Update(ref state);

            transformType.Update(ref state);

            ghostOwnerType.Update(ref state);
            staticAsteroidType.Update(ref state);
            sphereType.Update(ref state);
            playerIdType.Update(ref state);
            entityType.Update(ref state);

            commandTarget.Update(ref state);
            linkedEntityGroupFromEntity.Update(ref state);
            
            int chunkCount = asteroidQuery.CalculateChunkCountWithoutFiltering();
            int maxThreadCount = JobsUtility.ThreadIndexCount;

            var asteroidDestroyCounter = CollectionHelper.CreateNativeArray<int>(maxThreadCount, Allocator.TempJob);

            var asteroidJob = new DestroyAsteroidJob
            {
                commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                bulletChunks = bulletQuery.ToArchetypeChunkListAsync(state.WorldUpdateAllocator, out bulletHandle),
                bulletAgeType = bulletAgeType,

                transformType = transformType,
                asteroidDestructCounter = asteroidDestroyCounter,

                staticAsteroidType = staticAsteroidType,
                sphereType = sphereType,
                entityType = entityType,
                level = level,
                tick = networkTime.ServerTick,
                fixedDeltaTime = tickRate.SimulationFixedTimeStep,
            };

            var serverSettings =
                settingsQuery.ToComponentDataListAsync<ServerSettings>(state.WorldUpdateAllocator,
                    out settingsHandle);
            var shipJob = new DestroyShipJob
            {
                commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                asteroidChunks = asteroidQuery.ToArchetypeChunkListAsync(state.WorldUpdateAllocator, out asteroidHandle),
                bulletChunks = asteroidJob.bulletChunks,
                bulletAgeType = asteroidJob.bulletAgeType,

                transformType = asteroidJob.transformType,

                ghostOwnerType = ghostOwnerType,
                staticAsteroidType = asteroidJob.staticAsteroidType,
                sphereType = asteroidJob.sphereType,
                playerIdType = playerIdType,
                entityType = asteroidJob.entityType,
                serverSettings = serverSettings,
                playerClearQueue = playerClearQueue.AsParallelWriter(),
                level = asteroidJob.level,
                tick = networkTime.ServerTick,
                fixedDeltaTime = tickRate.SimulationFixedTimeStep,
            };
            var asteroidDep = JobHandle.CombineDependencies(state.Dependency, bulletHandle, levelHandle);
            var shipDep = JobHandle.CombineDependencies(asteroidDep, asteroidHandle, settingsHandle);

            var h1 = asteroidJob.ScheduleParallel(asteroidQuery, asteroidDep);
            var h2 = shipJob.ScheduleParallel(shipQuery, shipDep);

            var cleanupShipJob = new ClearShipPointerJob
            {
                playerClearQueue = playerClearQueue,
                commandTarget = commandTarget,
                linkedEntityGroupFromEntity = linkedEntityGroupFromEntity
            };

            var asteroidScore = SystemAPI.GetSingleton<AsteroidScore>();

            var updateScore = new GatherAsteroidDestructCounter
            {
                commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                asteroidDestructCounter = asteroidDestroyCounter,
                currentScore = asteroidScore.Value,
                scoreSingleton = SystemAPI.GetSingletonEntity<AsteroidScore>()
            };
            var scoreHandle = updateScore.Schedule(dependsOn: h1);
            var cleanupHandle = asteroidDestroyCounter.Dispose(scoreHandle);

            var handle = JobHandle.CombineDependencies(h1, h2, cleanupHandle);
            state.Dependency = JobHandle.CombineDependencies(cleanupShipJob.Schedule(h2), handle);
        }

        private static bool Intersect(float firstRadius, float secondRadius, float2 firstPos, float2 secondPos)
        {
            float2 diff = firstPos - secondPos;
            float distSq = math.dot(diff, diff);
            return distSq <= (firstRadius + secondRadius) * (firstRadius + secondRadius);
        }
    }
}
