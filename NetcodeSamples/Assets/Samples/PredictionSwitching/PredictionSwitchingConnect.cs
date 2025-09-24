using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PredictionSwitchingConnectClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PredictionSwitchingSettings>();
    }
    protected override void OnUpdate()
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach(var (_, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
        {
            commandBuffer.AddComponent<NetworkStreamInGame>(entity);
        }
        commandBuffer.Playback(EntityManager);
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PredictionSwitchingConnectServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PredictionSwitchingSettings>();
    }
    protected override void OnUpdate()
    {
        var settings = SystemAPI.GetSingleton<PredictionSwitchingSettings>();
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach(var (target, netId, entity) in
                SystemAPI.Query<RefRW<CommandTarget>, RefRO<NetworkId>>()
                    .WithNone<NetworkStreamInGame>()
                    .WithEntityAccess())
        {
            target.ValueRW.targetEntity = EntityManager.Instantiate(settings.Player);
            EntityManager.SetComponentData(target.ValueRW.targetEntity, new GhostOwner{NetworkId = netId.ValueRO.Value});

            // Spawn at the edge of the field, in a line.
            var isEven = (netId.ValueRO.Value & 1) == 0;
            const float halfCharacterSpawnSeparation = 1.3f;
            const float spawnStaggeredOffset = 0.65f;
            var staggeredXPos = netId.ValueRO.Value * math.@select(halfCharacterSpawnSeparation, -halfCharacterSpawnSeparation, isEven) + math.@select(-spawnStaggeredOffset, spawnStaggeredOffset, isEven);
            const float halfCapsuleHeight = 1f;
            const float nearMapWall = -22.5f;

            var transform = EntityManager.GetComponentData<LocalTransform>(target.ValueRW.targetEntity);
            EntityManager.SetComponentData(target.ValueRW.targetEntity,
                transform.WithPosition(new float3(staggeredXPos, halfCapsuleHeight, nearMapWall)));

            commandBuffer.AddComponent(entity, default(NetworkStreamInGame));
            // Add the player to the linked entity group so it is destroyed automatically on disconnect
            EntityManager.GetBuffer<LinkedEntityGroup>(entity).Add(new LinkedEntityGroup{Value = target.ValueRW.targetEntity});
        }
        commandBuffer.Playback(EntityManager);
    }
}
