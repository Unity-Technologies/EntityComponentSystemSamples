using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.NetCode;
using Unity.Mathematics;

namespace Asteroids.Server
{
    public struct LevelRequestedTag : IComponentData
    {
    }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateBefore(typeof(RpcSystem))]
    public partial struct LoadLevelSystem : ISystem
    {
        private EntityQuery m_LevelGroup;
        private PortableFunctionPointer<GhostImportance.BatchScaleImportanceDelegate> m_BatchScaleFunction;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<LevelComponent>();
            m_LevelGroup = state.GetEntityQuery(builder);

            state.RequireForUpdate<ServerSettings>();
            m_BatchScaleFunction = GhostDistanceImportance.BatchScaleWithRelevancyFunctionPointer;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = SystemAPI.GetSingleton<ServerSettings>();
            var hasGhostImportanceScaling = SystemAPI.TryGetSingletonEntity<GhostImportance>(out var ghostImportanceEntity);
            if (settings.levelData.enableGhostImportanceScaling != hasGhostImportanceScaling)
            {
                if (hasGhostImportanceScaling)
                {
                    state.EntityManager.DestroyEntity(ghostImportanceEntity);
                }
                else
                {
                    var gridSingleton = state.EntityManager.CreateSingleton(settings.levelData.distanceImportanceTileConfig);
                    state.EntityManager.AddComponentData(gridSingleton, new GhostImportance
                    {
                        BatchScaleImportanceFunction = m_BatchScaleFunction,
                        GhostConnectionComponentType = ComponentType.ReadOnly<GhostConnectionPosition>(),
                        GhostImportanceDataType = ComponentType.ReadOnly<GhostDistanceData>(),
                        GhostImportancePerChunkDataType = ComponentType.ReadOnly<GhostDistancePartitionShared>(),
                    });
                }
            }

            if (m_LevelGroup.IsEmptyIgnoreFilter)
            {
                var newLevel = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(newLevel, settings.levelData);
                return;
            }

            var level = m_LevelGroup.ToComponentDataListAsync<LevelComponent>(state.WorldUpdateAllocator,
                out var levelHandle);

            var levelJob = new LoadLevelJob
            {
                ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged),
                level = level,
            };
            state.Dependency = JobHandle.CombineDependencies(state.Dependency, levelHandle);
            levelJob.Schedule();
        }

        [BurstCompile]
        [WithNone(typeof(LevelRequestedTag))]
        internal partial struct LoadLevelJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public NativeList<LevelComponent> level;

            public void Execute(Entity entity, in NetworkId networkId)
            {
                ecb.AddComponent(entity, new LevelRequestedTag());
                var req = ecb.CreateEntity();
                ecb.AddComponent(req, new LevelLoadRequest
                {
                    levelData = level[0],
                });
                ecb.AddComponent(req, new SendRpcCommandRequest {TargetConnection = entity});
            }
        }
    }

}
