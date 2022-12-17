using Unity.Assertions;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct TriggerVolumePortal : IComponentData
{
    public Entity Companion;

    // When an entity is teleported to it's companion,
    // we increase companion's TransferCount so that
    // the enitty doesn't get immediately teleported
    // back to the original portal
    public int TransferCount;
}

public class TriggerVolumePortalAuthoring : MonoBehaviour
{
    public PhysicsBodyAuthoring CompanionPortal;
}

class TriggerVolumePortalAuthoringBaker : Baker<TriggerVolumePortalAuthoring>
{
    public override void Bake(TriggerVolumePortalAuthoring authoring)
    {
        var companion = GetEntity(authoring.CompanionPortal);
        AddComponent(new TriggerVolumePortal
        {
            Companion = companion,
            TransferCount = 0
        });
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct TriggerVolumePortalSystem : ISystem
{
    private EntityQuery m_HierarchyChildQuery;
    private EntityQuery m_NonTriggerDynamicBodyQuery;
    private EntityQueryMask m_HierarchyChildMask;
    private EntityQueryMask m_NonTriggerDynamicBodyMask;
    private TriggerVolumePortalComponentLookup _mComponentLookup;

    struct TriggerVolumePortalComponentLookup
    {
        public TriggerVolumePortalComponentLookup(ref SystemState systemState)
        {
            LocalToWorldData = systemState.GetComponentLookup<LocalToWorld>(false);
#if !ENABLE_TRANSFORM_V1
            LocalTransformData = systemState.GetComponentLookup<LocalTransform>(false);
#else
            PositionData = systemState.GetComponentLookup<Translation>(false);
            RotationData = systemState.GetComponentLookup<Rotation>(false);
#endif
            TriggerVolumePortalData = systemState.GetComponentLookup<TriggerVolumePortal>(false);
            PhysicsVelocityData = systemState.GetComponentLookup<PhysicsVelocity>(false);
            PhysicsGraphicalSmoothingData = systemState.GetComponentLookup<PhysicsGraphicalSmoothing>(false);
        }

        public void Update(ref SystemState systemState)
        {
            LocalToWorldData.Update(ref systemState);
#if !ENABLE_TRANSFORM_V1
            LocalTransformData.Update(ref systemState);
#else
            PositionData.Update(ref systemState);
            RotationData.Update(ref systemState);
#endif
            TriggerVolumePortalData.Update(ref systemState);
            PhysicsVelocityData.Update(ref systemState);
            PhysicsGraphicalSmoothingData.Update(ref systemState);
        }

        public ComponentLookup<LocalToWorld> LocalToWorldData;
#if !ENABLE_TRANSFORM_V1
        public ComponentLookup<LocalTransform> LocalTransformData;
#else
        public ComponentLookup<Translation> PositionData;
        public ComponentLookup<Rotation> RotationData;
#endif
        public ComponentLookup<TriggerVolumePortal> TriggerVolumePortalData;
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityData;
        public ComponentLookup<PhysicsGraphicalSmoothing> PhysicsGraphicalSmoothingData;
    }

    [BurstCompile]
    partial struct TriggerVolumePortalJob : IJobEntity
    {
        public TriggerVolumePortalComponentLookup ComponentDatas;
        public EntityQueryMask HierarchyChildMask;
        public EntityQueryMask NonTriggerDynamicBodyMask;

        [BurstCompile]
        public void Execute(Entity portalEntity, ref DynamicBuffer<StatefulTriggerEvent> triggerBuffer)
        {
            if (!ComponentDatas.TriggerVolumePortalData.HasComponent(portalEntity))
            {
                return;
            }

            var triggerVolumePortal = ComponentDatas.TriggerVolumePortalData[portalEntity];
            var companionEntity = triggerVolumePortal.Companion;
            var companionTriggerVolumePortal = ComponentDatas.TriggerVolumePortalData[companionEntity];

            for (int i = 0; i < triggerBuffer.Length; i++)
            {
                var triggerEvent = triggerBuffer[i];
                var otherEntity = triggerEvent.GetOtherEntity(portalEntity);

                // exclude other triggers, static bodies and processed events
                if (triggerEvent.State != StatefulEventState.Enter || !NonTriggerDynamicBodyMask.MatchesIgnoreFilter(otherEntity))
                {
                    continue;
                }

                // Check if entity just teleported to this portal,
                // and if it did, decrement TransferCount
                if (triggerVolumePortal.TransferCount != 0)
                {
                    triggerVolumePortal.TransferCount--;
                    continue;
                }

                // a static body may be in a hierarchy, in which case Translation and Rotation may not be in world space
                var portalTransform = HierarchyChildMask.MatchesIgnoreFilter(portalEntity)
                    ? Math.DecomposeRigidBodyTransform(ComponentDatas.LocalToWorldData[portalEntity].Value)
#if !ENABLE_TRANSFORM_V1
                    : new RigidTransform(ComponentDatas.LocalTransformData[portalEntity].Rotation, ComponentDatas.LocalTransformData[portalEntity].Position);
#else
                    : new RigidTransform(ComponentDatas.RotationData[portalEntity].Value, ComponentDatas.PositionData[portalEntity].Value);
#endif
                var companionTransform = HierarchyChildMask.MatchesIgnoreFilter(companionEntity)
                    ? Math.DecomposeRigidBodyTransform(ComponentDatas.LocalToWorldData[companionEntity].Value)
#if !ENABLE_TRANSFORM_V1
                    : new RigidTransform(ComponentDatas.LocalTransformData[companionEntity].Rotation, ComponentDatas.LocalTransformData[companionEntity].Position);
#else
                    : new RigidTransform(ComponentDatas.RotationData[companionEntity].Value, ComponentDatas.PositionData[companionEntity].Value);
#endif

                var portalPositionOffset = companionTransform.pos - portalTransform.pos;
                var portalRotationOffset = math.mul(companionTransform.rot, math.inverse(portalTransform.rot));

#if !ENABLE_TRANSFORM_V1
                var entityLocalTransformComponent = ComponentDatas.LocalTransformData[otherEntity];
#else
                var entityPositionComponent = ComponentDatas.PositionData[otherEntity];
                var entityRotationComponent = ComponentDatas.RotationData[otherEntity];
#endif
                var entityVelocityComponent = ComponentDatas.PhysicsVelocityData[otherEntity];

                entityVelocityComponent.Linear = math.rotate(portalRotationOffset, entityVelocityComponent.Linear);
#if !ENABLE_TRANSFORM_V1
                entityLocalTransformComponent.Position += portalPositionOffset;
                entityLocalTransformComponent.Rotation = math.mul(entityLocalTransformComponent.Rotation, portalRotationOffset);

                ComponentDatas.LocalTransformData[otherEntity] = entityLocalTransformComponent;
#else
                entityPositionComponent.Value += portalPositionOffset;
                entityRotationComponent.Value = math.mul(entityRotationComponent.Value, portalRotationOffset);

                ComponentDatas.PositionData[otherEntity] = entityPositionComponent;
                ComponentDatas.RotationData[otherEntity] = entityRotationComponent;
#endif
                ComponentDatas.PhysicsVelocityData[otherEntity] = entityVelocityComponent;

                if (ComponentDatas.PhysicsGraphicalSmoothingData.HasComponent(otherEntity))
                {
                    var entitySmoothingComponent = ComponentDatas.PhysicsGraphicalSmoothingData[otherEntity];
                    entitySmoothingComponent.ApplySmoothing = 0;
                    ComponentDatas.PhysicsGraphicalSmoothingData[otherEntity] = entitySmoothingComponent;
                }

                companionTriggerVolumePortal.TransferCount++;
            }

            ComponentDatas.TriggerVolumePortalData[portalEntity] = triggerVolumePortal;
            ComponentDatas.TriggerVolumePortalData[companionEntity] = companionTriggerVolumePortal;
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
            .WithAll<Parent, LocalToWorld>();
        m_HierarchyChildQuery = state.GetEntityQuery(builder);

        Assert.IsFalse(m_HierarchyChildQuery.HasFilter(), "The use of EntityQueryMask in this system will not respect the query's active filter settings.");
        m_HierarchyChildMask = m_HierarchyChildQuery.GetEntityQueryMask();

        builder = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
#if !ENABLE_TRANSFORM_V1
            .WithAll<LocalTransform, PhysicsVelocity>()
#else
                .WithAll<Translation, Rotation, PhysicsVelocity>()
#endif
            .WithNone<StatefulTriggerEvent>();
        m_NonTriggerDynamicBodyQuery = state.GetEntityQuery(builder);

        Assert.IsFalse(m_NonTriggerDynamicBodyQuery.HasFilter(), "The use of EntityQueryMask in this system will not respect the query's active filter settings.");
        m_NonTriggerDynamicBodyMask = m_NonTriggerDynamicBodyQuery.GetEntityQueryMask();

        _mComponentLookup = new TriggerVolumePortalComponentLookup(ref state);

        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadWrite<TriggerVolumePortal>()));
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _mComponentLookup.Update(ref state);

        state.Dependency = new TriggerVolumePortalJob()
        {
            ComponentDatas = _mComponentLookup,
            HierarchyChildMask = m_HierarchyChildMask,
            NonTriggerDynamicBodyMask = m_NonTriggerDynamicBodyMask
        }.Schedule(state.Dependency);
    }
}
