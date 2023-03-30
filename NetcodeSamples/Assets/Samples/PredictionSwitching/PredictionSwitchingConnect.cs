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
        Entities
            .WithStructuralChanges()
            .WithoutBurst()
            .WithNone<NetworkStreamInGame>()
            .WithAll<NetworkId>()
            .ForEach((Entity entity) =>
        {
            EntityManager.AddComponentData(entity, default(NetworkStreamInGame));
        }).Run();
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
        Entities
            .WithStructuralChanges()
            .WithoutBurst()
            .WithNone<NetworkStreamInGame>()
            .ForEach((Entity entity, ref CommandTarget target, in NetworkId netId) =>
        {
            target.targetEntity = EntityManager.Instantiate(settings.Player);
            EntityManager.SetComponentData(target.targetEntity, new GhostOwner{NetworkId = netId.Value});

            // Spawn at the edge of the field, in a line.
            var isEven = (netId.Value & 1) == 0;
            const float halfCharacterSpawnSeparation = 1.3f;
            const float spawnStaggeredOffset = 0.65f;
            var staggeredXPos = netId.Value * math.@select(halfCharacterSpawnSeparation, -halfCharacterSpawnSeparation, isEven) + math.@select(-spawnStaggeredOffset, spawnStaggeredOffset, isEven);
            const float halfCapsuleHeight = 1f;
            const float nearMapWall = -22.5f;

            var transform = EntityManager.GetComponentData<LocalTransform>(target.targetEntity);
            EntityManager.SetComponentData(target.targetEntity,
                transform.WithPosition(new float3(staggeredXPos, halfCapsuleHeight, nearMapWall)));

            EntityManager.AddComponentData(entity, default(NetworkStreamInGame));
            // Add the player to the linked entity group so it is destroyed automatically on disconnect
            EntityManager.GetBuffer<LinkedEntityGroup>(entity).Add(new LinkedEntityGroup{Value = target.targetEntity});
        }).Run();
    }
}
