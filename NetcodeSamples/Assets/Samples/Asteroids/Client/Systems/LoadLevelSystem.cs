using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Asteroids.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateBefore(typeof(RpcSystem))]
    [CreateAfter(typeof(RpcSystem))]
    public partial class LoadLevelSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem m_Barrier;
        private RpcQueue<RpcLevelLoaded, RpcLevelLoaded> m_RpcQueue;
        private Entity m_LevelSingleton;

        protected override void OnCreate()
        {
            m_Barrier = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            m_RpcQueue = SystemAPI.GetSingleton<RpcCollection>().GetRpcQueue<RpcLevelLoaded, RpcLevelLoaded>();

            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<LevelLoadRequest>(), ComponentType.ReadOnly<ReceiveRpcCommandRequest>()));

            // This is just here to make sure the subscen is streamed in before the client sets up the level data
            RequireForUpdate<AsteroidsSpawner>();
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.HasSingleton<LevelComponent>())
            {
                // The level always exist, "loading" just resizes it
                m_LevelSingleton = EntityManager.CreateEntity();
                EntityManager.AddComponentData(m_LevelSingleton, new LevelComponent {levelWidth = 0, levelHeight = 0});
            }
            else if (m_LevelSingleton == default) // in single world host mode, LevelComponent is created by the server system
            {
                m_LevelSingleton = SystemAPI.GetSingletonEntity<LevelComponent>();
            }
            var commandBuffer = m_Barrier.CreateCommandBuffer().AsParallelWriter();
            var rpcFromEntity = GetBufferLookup<OutgoingRpcDataStreamBuffer>();
            var ghostFromEntity = GetComponentLookup<GhostInstance>(true);
            var levelFromEntity = GetComponentLookup<LevelComponent>();
            var levelSingleton = m_LevelSingleton;
            var rpcQueue = m_RpcQueue;
            Dependency = new LoadLevelStepJob()
            {
                commandBuffer = commandBuffer,
                rpcFromEntity = rpcFromEntity,
                ghostFromEntity = ghostFromEntity,
                levelFromEntity = levelFromEntity,
                levelSingleton = levelSingleton,
                rpcQueue = rpcQueue,
            }.Schedule(Dependency);
            m_Barrier.AddJobHandleForProducer(Dependency);

            Dependency = new LoadLevelJob()
            {
                levelFromEntity = levelFromEntity,
                levelSingleton = levelSingleton,
            }.Schedule(Dependency);
        }
    }

    public partial struct LoadLevelStepJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter commandBuffer;
        public BufferLookup<OutgoingRpcDataStreamBuffer> rpcFromEntity;
        [ReadOnly] public ComponentLookup<GhostInstance> ghostFromEntity;
        public ComponentLookup<LevelComponent> levelFromEntity;
        public Entity levelSingleton;
        public RpcQueue<RpcLevelLoaded, RpcLevelLoaded> rpcQueue;
        public void Execute(Entity entity, [EntityIndexInChunk] int entityIndexInChunk, in LevelLoadRequest request, in ReceiveRpcCommandRequest requestSource)
        {
            commandBuffer.DestroyEntity(entityIndexInChunk, entity);

            // Check for disconnects
            if (!rpcFromEntity.HasBuffer(requestSource.SourceConnection))
                return;

            // set the level size - fake loading of level
            levelFromEntity[levelSingleton] = request.levelData;

            commandBuffer.AddComponent(entityIndexInChunk, requestSource.SourceConnection, new PlayerStateComponentData());
            commandBuffer.AddComponent(entityIndexInChunk, requestSource.SourceConnection, default(NetworkStreamInGame));
            commandBuffer.AddComponent(entityIndexInChunk, requestSource.SourceConnection, default(GhostConnectionPosition));
            rpcQueue.Schedule(rpcFromEntity[requestSource.SourceConnection], ghostFromEntity, new RpcLevelLoaded());
        }
    }

    partial struct LoadLevelJob : IJobEntity
    {
        public ComponentLookup<LevelComponent> levelFromEntity;
        public Entity levelSingleton;

        public void Execute(ref LocalTransform trans, ref PostTransformMatrix scaleMatrix, in LevelBorder border)
        {
            var scale = new float3(1);
            var level = levelFromEntity[levelSingleton];
            if (border.Side == 0)
            {
                trans.Position.x = level.levelWidth / 2f;
                trans.Position.y = 1;
                scale.x = level.levelWidth;
                scale.y = 2;
            }
            else if (border.Side == 1)
            {
                trans.Position.x = level.levelWidth / 2f;
                trans.Position.y = level.levelHeight - 1;
                scale.x = level.levelWidth;
                scale.y = 2;
            }
            else if (border.Side == 2)
            {
                trans.Position.x = 1;
                trans.Position.y = level.levelHeight / 2f;
                scale.x = 2;
                scale.y = level.levelHeight;
            }
            else if (border.Side == 3)
            {
                trans.Position.x = level.levelWidth - 1;
                trans.Position.y = level.levelHeight / 2f;
                scale.x = 2;
                scale.y = level.levelHeight;
            }

            scaleMatrix.Value = float4x4.Scale(scale.x, scale.y, scale.z);
        }
    }
}
