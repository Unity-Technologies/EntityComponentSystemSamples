using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

public enum LevelSyncState
{
    Idle,
    LevelLoadRequest,
    LevelLoadInProgress,
    LevelLoaded
}

public struct ClientLoadLevel : IRpcCommand
{
    public int LevelIndex;
}

public struct ClientReady : IRpcCommand
{
    public int LevelIndex;
}

// Add to network connections when level load sync starts, clients without this when loading is done are new connections
public struct LevelLoadingInProgress : IComponentData { }

// Add when connection/client is done loading so in progress count should equal done count when server can start
public struct LevelLoadingDone : IComponentData { }

public struct LevelSyncStateComponent : IComponentData
{
    public LevelSyncState State;
    public int CurrentLevel;
    // When client state is LevelLoadInProgress this level should be loaded
    public int NextLevel;
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[CreateBefore(typeof(LevelLoader))]
public partial class NetcodeClientLevelSync : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
        EntityManager.CreateEntity(typeof(LevelSyncStateComponent));
    }

    protected override void OnUpdate()
    {
        var connectionEntity = SystemAPI.GetSingletonEntity<NetworkId>();
        var levelState = SystemAPI.GetSingleton<LevelSyncStateComponent>();
        if (!SystemAPI.QueryBuilder().WithAll<ClientLoadLevel, ReceiveRpcCommandRequest>().Build().IsEmptyIgnoreFilter)
        {
            FixedString64Bytes worldName = World.Name;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            // When load level command arrives, disable ghost sync, unload current level and load specified level
            foreach (var (level, entity) in SystemAPI.Query<RefRO<ClientLoadLevel>>().WithEntityAccess()
                         .WithAll<ReceiveRpcCommandRequest>())
            {
                UnityEngine.Debug.Log($"[{worldName}] received command to load {level.ValueRO.LevelIndex}");
                ecb.RemoveComponent<NetworkStreamInGame>(connectionEntity);
                levelState.State = LevelSyncState.LevelLoadRequest;
                levelState.NextLevel = level.ValueRO.LevelIndex;
                SystemAPI.SetSingleton(levelState);
                ecb.DestroyEntity(entity);
            }
            ecb.Playback(EntityManager);
        }

        if (levelState.State == LevelSyncState.LevelLoaded)
        {
            if (!EntityManager.HasComponent<NetworkStreamInGame>(connectionEntity))
            {
                var netId = SystemAPI.GetSingleton<NetworkId>();
                UnityEngine.Debug.Log($"{World.Name} enable sync on connection {netId.Value}");
                EntityManager.AddComponent<NetworkStreamInGame>(connectionEntity);
            }

            UnityEngine.Debug.Log($"[{World.Name}] notifying server it's finished loading {levelState.CurrentLevel}");
            var rpcCmd = EntityManager.CreateEntity(typeof(ClientReady), typeof(SendRpcCommandRequest));
            EntityManager.AddComponentData(rpcCmd, new ClientReady(){LevelIndex = levelState.CurrentLevel});

            levelState.State = LevelSyncState.Idle;
            SystemAPI.SetSingleton(levelState);
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[CreateBefore(typeof(LevelLoader))]
public partial class NetcodeServerLevelSync : SystemBase
{
    private EntityQuery m_ClientsReadyQuery;
    private EntityQuery m_ClientsLoadingQuery;

    protected override void OnCreate()
    {
        m_ClientsReadyQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<LevelLoadingDone>());
        m_ClientsLoadingQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<LevelLoadingInProgress>());

        RequireForUpdate<NetworkId>();
        EntityManager.CreateEntity(typeof(LevelSyncStateComponent));
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var connections = GetComponentLookup<NetworkId>();
        var loadingInProgress = GetComponentLookup<LevelLoadingInProgress>();
        // TODO: Level number not being used for anything atm
        Entities.ForEach((Entity entity, in ClientReady level, in ReceiveRpcCommandRequest req) =>
        {
            UnityEngine.Debug.Log($"Client {connections[req.SourceConnection].Value} finished loading {level.LevelIndex}.");
            ecb.AddComponent<LevelLoadingDone>(req.SourceConnection);
            if (!loadingInProgress.HasComponent(req.SourceConnection))
                UnityEngine.Debug.LogError("Ready client was never marked as starting level loading");
            ecb.DestroyEntity(entity);
        }).Run();
        ecb.Playback(EntityManager);

        var readyCount = m_ClientsReadyQuery.CalculateEntityCount();
        var loadingCount = m_ClientsLoadingQuery.CalculateEntityCount();

        // All scenes finished loading and clients are ready
        var levelState = SystemAPI.GetSingleton<LevelSyncStateComponent>();
        if (levelState.State == LevelSyncState.LevelLoaded && loadingCount == readyCount)
        {
            UnityEngine.Debug.Log("Server subscenes finished loading and all clients are ready");

            var conQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
            var cons = conQuery.ToEntityArray(Allocator.Temp);
            var conIds = conQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);
            for (int i = 0; i < cons.Length; ++i)
            {
                if (!EntityManager.HasComponent<NetworkStreamInGame>(cons[i]))
                {
                    UnityEngine.Debug.Log($"[{World.Name}] Enable sync on {conIds[i].Value}");
                    EntityManager.AddComponent<NetworkStreamInGame>(cons[i]);
                }
            }

            levelState.State = LevelSyncState.Idle;
            SystemAPI.SetSingleton(levelState);
            ecb = new EntityCommandBuffer(Allocator.Temp);
            FixedString64Bytes world = World.Name;
            Entities.WithAll<NetworkId>().ForEach((Entity entity) =>
            {
                ecb.RemoveComponent<LevelLoadingInProgress>(entity);
                ecb.RemoveComponent<LevelLoadingDone>(entity);
            }).Run();
            ecb.Playback(EntityManager);
        }
    }
}

public static class NetcodeLevelSync
{
    public static void TriggerClientLoadLevel(int level, World serverWorld)
    {
        // Trigger level load on all clients
        var rpcCmd = serverWorld.EntityManager.CreateEntity();
        serverWorld.EntityManager.AddComponentData(rpcCmd, new ClientLoadLevel(){LevelIndex = level});
        serverWorld.EntityManager.AddComponent<SendRpcCommandRequest>(rpcCmd);

        // Mark each connection as being in progress of loading
        var connectionsQuery =
            serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
        var connectionEntities = connectionsQuery.ToEntityArray(Allocator.Temp);
        foreach (var connection in connectionEntities)
        {
            serverWorld.EntityManager.AddComponentData(connection, new LevelLoadingInProgress());
        }
    }

    public static void SetLevelState(LevelSyncState state, World world)
    {
        var levelStateQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<LevelSyncStateComponent>());
        var levelStateEntity = levelStateQuery.ToEntityArray(Allocator.Temp);
        var levelStateData = levelStateQuery.ToComponentDataArray<LevelSyncStateComponent>(Allocator.Temp);
        var levelState = levelStateData[0];
        levelState.State = state;
        world.EntityManager.SetComponentData(levelStateEntity[0], levelState);

    }
}
