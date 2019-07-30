using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Transforms;
using Unity.Physics.Authoring;
using Unity.Mathematics;
using Unity.Burst;

public struct TriggerVolumePortal : IComponentData
{
    public Entity Companion;
}

public class TriggerVolumePortalBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public PhysicsBody CompanionPortal;

    void OnEnable() { }

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            var companion = conversionSystem.GetPrimaryEntity(CompanionPortal);
            dstManager.AddComponentData(entity, new TriggerVolumePortal()
            {
                Companion = companion,
            });
        }
    }
}



[UpdateBefore(typeof(BuildPhysicsWorld))]
public class TriggerVolumePortalSystem : JobComponentSystem
{
    public EntityQuery m_OverlappingGroup;

    protected override void OnCreate()
    {
        m_OverlappingGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(OverlappingTriggerVolume),
                typeof(PortalOverlappingTriggerVolume),
            }
        });
    }

    [BurstCompile]
    struct PortalOverlapUpdateJob : IJob
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> OverlappingEntities;
        public float DeltaTime;

        [ReadOnly] public ComponentDataFromEntity<TriggerVolume> TriggerComponents;
        [ReadOnly] public ComponentDataFromEntity<TriggerVolumePortal> PortalComponents;

        public ComponentDataFromEntity<OverlappingTriggerVolume> OverlappingComponents;
        public ComponentDataFromEntity<Translation> PositionComponents;
        public ComponentDataFromEntity<Rotation> RotationComponents;
        public ComponentDataFromEntity<PhysicsVelocity> VelocityComponents;

        public void Execute()
        {
            for ( int i = 0; i < OverlappingEntities.Length; i++)
            {
                var overlappingEntity = OverlappingEntities[i];

                var overlappingComponent = OverlappingComponents[overlappingEntity];
                var portalEntity = overlappingComponent.VolumeEntity;
                if (overlappingComponent.HasJustEntered)
                {
                    var companionEntity = PortalComponents[portalEntity].Companion;

                    var orangePosition = PositionComponents[portalEntity].Value;
                    var bluePosition = PositionComponents[companionEntity].Value;
                    var portalPositionOffset = bluePosition - orangePosition;

                    var orangeRotation = RotationComponents[portalEntity].Value;
                    var blueRotation = RotationComponents[companionEntity].Value;
                    var portalRotationOffset = math.mul(blueRotation, math.inverse(orangeRotation));

                    var entityPositionComponent = PositionComponents[overlappingEntity];
                    var entityRotationComponent = RotationComponents[overlappingEntity];
                    var entityVelocityComponent = VelocityComponents[overlappingEntity];

                    entityVelocityComponent.Linear = math.rotate(portalRotationOffset, entityVelocityComponent.Linear);
                    // Should look more into why velocity delta is needed to be removed from position.
                    // Likely an ordering issue with being a frame behind the entered event.
                    // TODO: also should get the actual timestep 
                    entityPositionComponent.Value += portalPositionOffset - entityVelocityComponent.Linear * DeltaTime;
                    entityRotationComponent.Value = math.mul(entityRotationComponent.Value, portalRotationOffset);

                    PositionComponents[overlappingEntity] = entityPositionComponent;
                    RotationComponents[overlappingEntity] = entityRotationComponent;
                    VelocityComponents[overlappingEntity] = entityVelocityComponent;

                    var companionTriggerComponent = TriggerComponents[companionEntity];
                    overlappingComponent.VolumeEntity = companionEntity;
                    OverlappingComponents[overlappingEntity] = overlappingComponent;
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var overlappingEntities = m_OverlappingGroup.ToEntityArray(Allocator.TempJob);

        var triggerComponents = GetComponentDataFromEntity<TriggerVolume>(true);
        var portalComponents = GetComponentDataFromEntity<TriggerVolumePortal>(true);

        var overlappingComponents = GetComponentDataFromEntity<OverlappingTriggerVolume>();
        var positionComponents = GetComponentDataFromEntity<Translation>();
        var rotationComponents = GetComponentDataFromEntity<Rotation>();
        var velocityComponents = GetComponentDataFromEntity<PhysicsVelocity>();

        JobHandle job = new PortalOverlapUpdateJob()
        {
            OverlappingEntities = overlappingEntities,
            DeltaTime = Time.fixedDeltaTime,

            OverlappingComponents = overlappingComponents,
            TriggerComponents = triggerComponents,
            PortalComponents = portalComponents,
            PositionComponents = positionComponents,
            RotationComponents = rotationComponents,
            VelocityComponents = velocityComponents,
        }.Schedule(inputDeps);

        return job;
    }
}


