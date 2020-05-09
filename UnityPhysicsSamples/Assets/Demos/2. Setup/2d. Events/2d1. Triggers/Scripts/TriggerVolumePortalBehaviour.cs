using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct TriggerVolumePortal : IComponentData
{
    public Entity Companion;
}

public class TriggerVolumePortalBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public PhysicsBodyAuthoring CompanionPortal;

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
    EntityQuery m_OverlappingGroup;
    EntityQueryMask m_HierarchyChildMask;

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
        m_HierarchyChildMask = EntityManager.GetEntityQueryMask(
            GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Parent),
                    typeof(LocalToWorld)
                }
            })
        );
    }

    [BurstCompile]
    struct PortalOverlapUpdateJob : IJob
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> OverlappingEntities;
        public float DeltaTime;

        [ReadOnly] public ComponentDataFromEntity<TriggerVolume> TriggerComponents;
        [ReadOnly] public ComponentDataFromEntity<TriggerVolumePortal> PortalComponents;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> LocalToWorldComponents;

        public EntityQueryMask HierarchyChildMask;

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

                    // a static body may be in a hierarchy, in which case Translation and Rotation may not be in world space
                    var portalTransform = HierarchyChildMask.Matches(portalEntity)
                        ? Math.DecomposeRigidBodyTransform(LocalToWorldComponents[portalEntity].Value)
                        : new RigidTransform(RotationComponents[portalEntity].Value, PositionComponents[portalEntity].Value);
                    var companionTransform = HierarchyChildMask.Matches(companionEntity)
                        ? Math.DecomposeRigidBodyTransform(LocalToWorldComponents[companionEntity].Value)
                        : new RigidTransform(RotationComponents[companionEntity].Value, PositionComponents[companionEntity].Value);

                    var portalPositionOffset = companionTransform.pos - portalTransform.pos;
                    var portalRotationOffset = math.mul(companionTransform.rot, math.inverse(portalTransform.rot));

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
        var localToWorldComponents = GetComponentDataFromEntity<LocalToWorld>();
        var positionComponents = GetComponentDataFromEntity<Translation>();
        var rotationComponents = GetComponentDataFromEntity<Rotation>();
        var velocityComponents = GetComponentDataFromEntity<PhysicsVelocity>();

        var job = new PortalOverlapUpdateJob
        {
            OverlappingEntities = overlappingEntities,
            DeltaTime = UnityEngine.Time.fixedDeltaTime,

            OverlappingComponents = overlappingComponents,
            TriggerComponents = triggerComponents,
            PortalComponents = portalComponents,
            LocalToWorldComponents = localToWorldComponents,
            HierarchyChildMask = m_HierarchyChildMask,
            PositionComponents = positionComponents,
            RotationComponents = rotationComponents,
            VelocityComponents = velocityComponents,
        }.Schedule(inputDeps);

        return job;
    }
}


