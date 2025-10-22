using System.Threading;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
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
    public partial struct CollisionSystem : ISystem
    {
        private EntityQuery shipQuery;
        private EntityQuery bulletQuery;
        private EntityQuery asteroidQuery;
        private EntityQuery m_LevelQuery;

        private NativeQueue<Entity> playerClearQueue;
        private EntityQuery settingsQuery;

        ComponentTypeHandle<LocalTransform> transformType;

        ComponentTypeHandle<GhostOwner> ghostOwnerType;
        ComponentTypeHandle<StaticAsteroid> staticAsteroidType;
        ComponentTypeHandle<PlayerIdComponentData> playerIdType;
        SharedComponentTypeHandle<GhostDistancePartitionShared> distancePartitionSharedType;
        EntityTypeHandle entityType;

        ComponentLookup<CommandTarget> commandTarget;
        BufferLookup<LinkedEntityGroup> linkedEntityGroupFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform, ShipTagComponentData, GhostOwner>();
            shipQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<LocalTransform, BulletTagComponent, BulletAgeComponent, GhostOwner>();

            bulletQuery = state.GetEntityQuery(builder);
            builder.Reset();
            builder.WithAll<LocalTransform, AsteroidTagComponentData>();
            asteroidQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<ServerSettings>();
            settingsQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<LevelComponent>();
            m_LevelQuery = state.GetEntityQuery(builder);

            playerClearQueue = new NativeQueue<Entity>(Allocator.Persistent);
            state.RequireForUpdate(m_LevelQuery);

            transformType = state.GetComponentTypeHandle<LocalTransform>(true);
            ghostOwnerType = state.GetComponentTypeHandle<GhostOwner>(true);
            staticAsteroidType = state.GetComponentTypeHandle<StaticAsteroid>(true);
            playerIdType = state.GetComponentTypeHandle<PlayerIdComponentData>(true);
            distancePartitionSharedType = state.GetSharedComponentTypeHandle<GhostDistancePartitionShared>();
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

        private static bool WithinTileBroadphase(int3 shipTile, int3 bulletTile)
        {
            var tileDistance = math.abs(shipTile - bulletTile);
            return math.all(tileDistance <= 1);
        }

        [BurstCompile]
        internal struct DestroyAsteroidJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public NativeList<ArchetypeChunk> bulletChunks;
            [ReadOnly] public ComponentTypeHandle<LocalTransform> transformType;
            [ReadOnly] public ComponentTypeHandle<StaticAsteroid> staticAsteroidType;
            [ReadOnly] public EntityTypeHandle entityType;
            [ReadOnly] public SharedComponentTypeHandle<GhostDistancePartitionShared> distancePartitionSharedType;

            [ReadOnly] public NativeList<LevelComponent> level;
            [NativeDisableUnsafePtrRestriction] public RefRW<AsteroidScore> asteroidScore;
            public NetworkTick tick;
            public uint simulationStepBatchSize;
            public float fixedDeltaTime;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                // Rate-limit this, as it gets incredibly expensive on high chunk counts.
                if ((chunk.SequenceNumber + tick.TickIndexForValidTick) % level[0].collisionSystemRoundRobinSegments != 0) return;

                var destroyedAsteroidCounter = 0;
                var shipTile = chunk.Has(distancePartitionSharedType) ? chunk.GetSharedComponent(distancePartitionSharedType).Index : 0;
                var asteroidEntity = chunk.GetNativeArray(entityType);
                var staticAsteroid = chunk.GetNativeArray(ref staticAsteroidType);
                if (staticAsteroid.IsCreated)
                {
                    for (int asteroid = 0; asteroid < asteroidEntity.Length; ++asteroid)
                    {
                        var firstPos = staticAsteroid[asteroid].GetPosition(tick, 1, fixedDeltaTime).xy;
                        CheckOutOfBounds(unfilteredChunkIndex, asteroidEntity[asteroid], firstPos, level[0].asteroidCollisionRadius);
                    }
                    for (int bc = 0; bc < bulletChunks.Length; ++bc)
                    {
                        var bulletChunk = bulletChunks[bc];
                        var bulletTile = bulletChunk.Has(distancePartitionSharedType) ? bulletChunk.GetSharedComponent(distancePartitionSharedType).Index : 0;
                        if (!WithinTileBroadphase(shipTile, bulletTile)) continue;

                        var bulletEntities = bulletChunk.GetNativeArray(entityType);
                        var bulletTrans = bulletChunk.GetNativeArray(ref transformType);
                        for (int asteroid = 0; asteroid < asteroidEntity.Length; ++asteroid)
                        {
                            var firstPos = staticAsteroid[asteroid].GetPosition(tick, 1, fixedDeltaTime).xy;
                            CheckCollisionsInner(unfilteredChunkIndex, asteroidEntity[asteroid], bulletEntities, bulletTrans, firstPos, ref destroyedAsteroidCounter);
                        }
                    }
                }
                else
                {
                    var asteroidPos = chunk.GetNativeArray(ref transformType);
                    for (int asteroid = 0; asteroid < asteroidPos.Length; ++asteroid)
                    {
                        var firstPos = asteroidPos[asteroid].Position.xy;
                        CheckOutOfBounds(unfilteredChunkIndex, asteroidEntity[asteroid], firstPos, level[0].asteroidCollisionRadius);
                    }
                    for (int bc = 0; bc < bulletChunks.Length; ++bc)
                    {
                        var bulletChunk = bulletChunks[bc];
                        var bulletTile = bulletChunk.Has(distancePartitionSharedType) ? bulletChunk.GetSharedComponent(distancePartitionSharedType).Index : 0;
                        if (!WithinTileBroadphase(shipTile, bulletTile)) continue;

                        var bulletEntities = bulletChunk.GetNativeArray(entityType);
                        var bulletTrans = bulletChunk.GetNativeArray(ref transformType);
                        for (int asteroid = 0; asteroid < asteroidPos.Length; ++asteroid)
                        {
                            var firstPos = asteroidPos[asteroid].Position.xy;
                            CheckCollisionsInner(unfilteredChunkIndex, asteroidEntity[asteroid], bulletEntities, bulletTrans, firstPos, ref destroyedAsteroidCounter);
                        }
                    }
                }

                // This sum theoretically causes thread contention, but said contention should be minimal (as bullets
                // destroying asteroids is relatively rare).
                if(destroyedAsteroidCounter > 0)
                    Interlocked.Add(ref asteroidScore.ValueRW.Value, destroyedAsteroidCounter);
            }

            private void CheckCollisionsInner(int unfilteredChunkIndex, in Entity asteroidEntity,
                NativeArray<Entity> bulletEntities, NativeArray<LocalTransform> bulletTrans,
                in float2 asteroidPos, ref int destroyedAsteroidCounter)
            {
                for (int bullet = 0; bullet < bulletEntities.Length; ++bullet)
                {
                    var secondPos = bulletTrans[bullet].Position.xy;
                    if (Intersect(level[0].bulletCollisionRadius, level[0].asteroidCollisionRadius, asteroidPos, secondPos))
                    {
                        commandBuffer.DestroyEntity(unfilteredChunkIndex, asteroidEntity);
                        destroyedAsteroidCounter++;

                        if (level[0].bulletsDestroyedOnContact)
                            commandBuffer.DestroyEntity(unfilteredChunkIndex, bulletEntities[bullet]);
                    }
                }
            }
            private void CheckOutOfBounds(int unfilteredChunkIndex, in Entity asteroidEntity, in float2 firstPos, float firstRadius)
            {
                if (Hint.Unlikely(firstPos.x - firstRadius < 0 || firstPos.y - firstRadius < 0 ||
                    firstPos.x + firstRadius > level[0].levelHeight ||
                    firstPos.y + firstRadius > level[0].levelHeight))
                {
                    commandBuffer.DestroyEntity(unfilteredChunkIndex, asteroidEntity);
                }
            }
        }
        [BurstCompile]
        internal struct DestroyShipJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public NativeList<ArchetypeChunk> asteroidChunks;
            [ReadOnly] public NativeList<ArchetypeChunk> bulletChunks;
            [ReadOnly] public ComponentTypeHandle<LocalTransform> transformType;
            [ReadOnly] public ComponentTypeHandle<GhostOwner> ghostOwnerType;
            [ReadOnly] public ComponentTypeHandle<StaticAsteroid> staticAsteroidType;
            [ReadOnly] public ComponentTypeHandle<PlayerIdComponentData> playerIdType;
            [ReadOnly] public SharedComponentTypeHandle<GhostDistancePartitionShared> distancePartitionSharedType;
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

                var shipTile = chunk.Has(distancePartitionSharedType) ? chunk.GetSharedComponent(distancePartitionSharedType).Index : 0;
                var shipTrans = chunk.GetNativeArray(ref transformType);
                var shipPlayerId = chunk.GetNativeArray(ref playerIdType);
                var shipEntity = chunk.GetNativeArray(entityType);
                var shipGhostOwner = chunk.GetNativeArray(ref ghostOwnerType);

                for (int ship = 0; ship < shipTrans.Length; ++ship)
                {
                    int alive = 1;
                    var firstPos = shipTrans[ship].Position.xy;

                    var firstRadius = level[0].shipCollisionRadius;
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
                        var secondRadius = level[0].bulletCollisionRadius;
                        for (int bc = 0; bc < bulletChunks.Length && alive != 0; ++bc)
                        {
                            var bulletChunk = bulletChunks[bc];
                            var bulletTile = bulletChunk.Has(distancePartitionSharedType) ? bulletChunk.GetSharedComponent(distancePartitionSharedType).Index : 0;
                            if (!WithinTileBroadphase(shipTile, bulletTile)) continue;

                            var bulletEntities = bulletChunks[bc].GetNativeArray(entityType);
                            var bulletPos = bulletChunks[bc].GetNativeArray(ref transformType);

                            var bulletGhostOwner = bulletChunks[bc].GetNativeArray(ref ghostOwnerType);
                            for (int bullet = 0; bullet < bulletEntities.Length; ++bullet)
                            {
                                if (bulletGhostOwner[bullet].NetworkId == shipNetworkId)
                                    continue;

                                var secondPos = bulletPos[bullet].Position.xy;
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
                        var secondRadius = level[0].asteroidCollisionRadius;
                        for (int ac = 0; ac < asteroidChunks.Length && alive != 0; ++ac)
                        {
                            var asteroidChunk = asteroidChunks[ac];
                            var asteroidTile = asteroidChunk.Has(distancePartitionSharedType) ? asteroidChunk.GetSharedComponent(distancePartitionSharedType).Index : 0;
                            if (!WithinTileBroadphase(shipTile, asteroidTile)) continue;

                            var asteroidEntity = asteroidChunk.GetNativeArray(entityType);
                            var staticAsteroid = asteroidChunk.GetNativeArray(ref staticAsteroidType);
                            if (staticAsteroid.IsCreated)
                            {
                                for (int asteroid = 0; asteroid < staticAsteroid.Length; ++asteroid)
                                {
                                    var secondPos = staticAsteroid[asteroid].GetPosition(tick, 1, fixedDeltaTime).xy;
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

                                var asteroidTrans = asteroidChunk.GetNativeArray(ref transformType);
                                for (int asteroid = 0; asteroid < asteroidTrans.Length; ++asteroid)
                                {
                                    var secondPos = asteroidTrans[asteroid].Position.xy;
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
        public void OnUpdate(ref SystemState state)
        {
            JobHandle bulletHandle;
            JobHandle asteroidHandle;
            JobHandle levelHandle;
            JobHandle settingsHandle;
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var asteroidScoreRef = SystemAPI.GetSingletonRW<AsteroidScore>();
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();

            var level = m_LevelQuery.ToComponentDataListAsync<LevelComponent>(state.WorldUpdateAllocator,
                out levelHandle);

            transformType.Update(ref state);

            ghostOwnerType.Update(ref state);
            staticAsteroidType.Update(ref state);
            playerIdType.Update(ref state);
            entityType.Update(ref state);
            distancePartitionSharedType.Update(ref state);

            commandTarget.Update(ref state);
            linkedEntityGroupFromEntity.Update(ref state);

            var asteroidJob = new DestroyAsteroidJob
            {
                commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                bulletChunks = bulletQuery.ToArchetypeChunkListAsync(state.WorldUpdateAllocator, out bulletHandle),
                transformType = transformType,
                asteroidScore = asteroidScoreRef,
                staticAsteroidType = staticAsteroidType,
                entityType = entityType,
                distancePartitionSharedType = distancePartitionSharedType,
                level = level,
                tick = networkTime.ServerTick,
                simulationStepBatchSize = (uint) networkTime.SimulationStepBatchSize,
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
                transformType = asteroidJob.transformType,
                ghostOwnerType = ghostOwnerType,
                staticAsteroidType = asteroidJob.staticAsteroidType,
                playerIdType = playerIdType,
                entityType = asteroidJob.entityType,
                distancePartitionSharedType = distancePartitionSharedType,
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

            JobHandle.ScheduleBatchedJobs(); // We call this because waiting for the above jobs to start can
                                             // often take significantly longer than their execution.

            var cleanupShipJob = new ClearShipPointerJob
            {
                playerClearQueue = playerClearQueue,
                commandTarget = commandTarget,
                linkedEntityGroupFromEntity = linkedEntityGroupFromEntity,
            };
            var h3 = cleanupShipJob.Schedule(h2);
            state.Dependency = JobHandle.CombineDependencies(h1, h2, h3);
        }

        private static bool Intersect(float firstRadius, float secondRadius, float2 firstPos, float2 secondPos)
        {
            float2 diff = firstPos - secondPos;
            float distSq = math.dot(diff, diff);
            return distSq <= (firstRadius + secondRadius) * (firstRadius + secondRadius);
        }
    }
}
