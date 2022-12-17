using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
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
#if !ENABLE_TRANSFORM_V1
        ComponentTypeHandle<LocalTransform> transformType;
#else
        ComponentTypeHandle<Translation> positionType;
#endif
        ComponentTypeHandle<GhostOwnerComponent> ghostOwnerType;
        ComponentTypeHandle<StaticAsteroid> staticAsteroidType;
        ComponentTypeHandle<CollisionSphereComponent> sphereType;
        ComponentTypeHandle<PlayerIdComponentData> playerIdType;
        EntityTypeHandle entityType;

        ComponentLookup<CommandTargetComponent> commandTarget;
        BufferLookup<LinkedEntityGroup> linkedEntityGroupFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
#if !ENABLE_TRANSFORM_V1
                .WithAll<LocalTransform, CollisionSphereComponent, ShipTagComponentData, GhostOwnerComponent>();
#else
                .WithAll<Translation, CollisionSphereComponent, ShipTagComponentData, GhostOwnerComponent>();
#endif
            shipQuery = state.GetEntityQuery(builder);

            builder.Reset();
#if !ENABLE_TRANSFORM_V1
            builder.WithAll<LocalTransform, CollisionSphereComponent, BulletTagComponent, BulletAgeComponent, GhostOwnerComponent>();
#else
            builder.WithAll<Translation, CollisionSphereComponent, BulletTagComponent, BulletAgeComponent, GhostOwnerComponent>();
#endif
            bulletQuery = state.GetEntityQuery(builder);

            builder.Reset();
#if !ENABLE_TRANSFORM_V1
            builder.WithAll<LocalTransform, CollisionSphereComponent, AsteroidTagComponentData>();
#else
            builder.WithAll<Translation, CollisionSphereComponent, AsteroidTagComponentData>();
#endif
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
#if !ENABLE_TRANSFORM_V1
            transformType = state.GetComponentTypeHandle<LocalTransform>(true);
#else
            positionType = state.GetComponentTypeHandle<Translation>(true);
#endif
            ghostOwnerType = state.GetComponentTypeHandle<GhostOwnerComponent>(true);
            staticAsteroidType = state.GetComponentTypeHandle<StaticAsteroid>(true);
            sphereType = state.GetComponentTypeHandle<CollisionSphereComponent>(true);
            playerIdType = state.GetComponentTypeHandle<PlayerIdComponentData>(true);
            entityType = state.GetEntityTypeHandle();

            commandTarget = state.GetComponentLookup<CommandTargetComponent>();
            linkedEntityGroupFromEntity = state.GetBufferLookup<LinkedEntityGroup>();
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
#if !ENABLE_TRANSFORM_V1
            [ReadOnly] public ComponentTypeHandle<LocalTransform> transformType;
#else
            [ReadOnly] public ComponentTypeHandle<Translation> positionType;
#endif
            [ReadOnly] public ComponentTypeHandle<StaticAsteroid> staticAsteroidType;
            [ReadOnly] public ComponentTypeHandle<CollisionSphereComponent> sphereType;
            [ReadOnly] public EntityTypeHandle entityType;

            [ReadOnly] public NativeList<LevelComponent> level;
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
#if !ENABLE_TRANSFORM_V1
                    var asteroidPos = chunk.GetNativeArray(ref transformType);
                    for (int asteroid = 0; asteroid < asteroidPos.Length; ++asteroid)
                    {
                        var firstPos = asteroidPos[asteroid].Position.xy;
#else
                    var asteroidPos = chunk.GetNativeArray(ref positionType);
                    for (int asteroid = 0; asteroid < asteroidPos.Length; ++asteroid)
                    {
                        var firstPos = asteroidPos[asteroid].Value.xy;
#endif
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
#if !ENABLE_TRANSFORM_V1
                    var bulletTrans = bulletChunks[bc].GetNativeArray(ref transformType);
#else
                    var bulletPos = bulletChunks[bc].GetNativeArray(ref positionType);
#endif
                    var bulletSphere = bulletChunks[bc].GetNativeArray(ref sphereType);
                    for (int bullet = 0; bullet < bulletAge.Length; ++bullet)
                    {
                        if (bulletAge[bullet].age > bulletAge[bullet].maxAge)
                            return;
#if !ENABLE_TRANSFORM_V1
                        var secondPos = bulletTrans[bullet].Position.xy;
#else
                        var secondPos = bulletPos[bullet].Value.xy;
#endif
                        var secondRadius = bulletSphere[bullet].radius;
                        if (Intersect(firstRadius, secondRadius, firstPos, secondPos))
                        {
                            commandBuffer.DestroyEntity(unfilteredChunkIndex, asteroidEntity);

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
#if !ENABLE_TRANSFORM_V1
            [ReadOnly] public ComponentTypeHandle<LocalTransform> transformType;
#else
            [ReadOnly] public ComponentTypeHandle<Translation> positionType;
#endif
            [ReadOnly] public ComponentTypeHandle<GhostOwnerComponent> ghostOwnerType;
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

#if !ENABLE_TRANSFORM_V1
                var shipTrans = chunk.GetNativeArray(ref transformType);
#else
                var shipPos = chunk.GetNativeArray(ref positionType);
#endif
                var shipSphere = chunk.GetNativeArray(ref sphereType);
                var shipPlayerId = chunk.GetNativeArray(ref playerIdType);
                var shipEntity = chunk.GetNativeArray(entityType);
                var shipGhostOwner = chunk.GetNativeArray(ref ghostOwnerType);

#if !ENABLE_TRANSFORM_V1
                for (int ship = 0; ship < shipTrans.Length; ++ship)
                {
                    int alive = 1;
                    var firstPos = shipTrans[ship].Position.xy;
#else
                for (int ship = 0; ship < shipPos.Length; ++ship)
                {
                    int alive = 1;
                    var firstPos = shipPos[ship].Value.xy;
#endif
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
#if !ENABLE_TRANSFORM_V1
                            var bulletPos = bulletChunks[bc].GetNativeArray(ref transformType);
#else
                            var bulletPos = bulletChunks[bc].GetNativeArray(ref positionType);
#endif
                            var bulletGhostOwner = bulletChunks[bc].GetNativeArray(ref ghostOwnerType);
                            var bulletSphere = bulletChunks[bc].GetNativeArray(ref sphereType);
                            for (int bullet = 0; bullet < bulletAge.Length; ++bullet)
                            {
                                if (bulletAge[bullet].age > bulletAge[bullet].maxAge || bulletGhostOwner[bullet].NetworkId == shipNetworkId)
                                    continue;
#if !ENABLE_TRANSFORM_V1
                                var secondPos = bulletPos[bullet].Position.xy;
#else
                                var secondPos = bulletPos[bullet].Value.xy;
#endif
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
#if !ENABLE_TRANSFORM_V1
                                var asteroidTrans = asteroidChunks[ac].GetNativeArray(ref transformType);
                                for (int asteroid = 0; asteroid < asteroidTrans.Length; ++asteroid)
                                {
                                    var secondPos = asteroidTrans[asteroid].Position.xy;
#else
                                var asteroidPos = asteroidChunks[ac].GetNativeArray(ref positionType);
                                for (int asteroid = 0; asteroid < asteroidPos.Length; ++asteroid)
                                {
                                    var secondPos = asteroidPos[asteroid].Value.xy;
#endif
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
            public ComponentLookup<CommandTargetComponent> commandTarget;
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
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();

            var level = m_LevelQuery.ToComponentDataListAsync<LevelComponent>(state.WorldUpdateAllocator,
                out levelHandle);

            bulletAgeType.Update(ref state);
#if !ENABLE_TRANSFORM_V1
            transformType.Update(ref state);
#else
            positionType.Update(ref state);
#endif
            ghostOwnerType.Update(ref state);
            staticAsteroidType.Update(ref state);
            sphereType.Update(ref state);
            playerIdType.Update(ref state);
            entityType.Update(ref state);

            commandTarget.Update(ref state);
            linkedEntityGroupFromEntity.Update(ref state);

            var asteroidJob = new DestroyAsteroidJob
            {
                commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                bulletChunks = bulletQuery.ToArchetypeChunkListAsync(state.WorldUpdateAllocator, out bulletHandle),
                bulletAgeType = bulletAgeType,
#if !ENABLE_TRANSFORM_V1
                transformType = transformType,
#else
                positionType = positionType,
#endif
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
#if !ENABLE_TRANSFORM_V1
                transformType = asteroidJob.transformType,
#else
                positionType = asteroidJob.positionType,
#endif
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

            var handle = JobHandle.CombineDependencies(h1, h2);

            var cleanupShipJob = new ClearShipPointerJob
            {
                playerClearQueue = playerClearQueue,
                commandTarget = commandTarget,
                linkedEntityGroupFromEntity = linkedEntityGroupFromEntity
            };
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
