using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;


[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
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

            LocalTransformData = systemState.GetComponentLookup<LocalTransform>(false);

            TriggerVolumePortalData = systemState.GetComponentLookup<TriggerVolumePortal>(false);
            PhysicsVelocityData = systemState.GetComponentLookup<PhysicsVelocity>(false);
            PhysicsGraphicalSmoothingData = systemState.GetComponentLookup<PhysicsGraphicalSmoothing>(false);
        }

        public void Update(ref SystemState systemState)
        {
            LocalToWorldData.Update(ref systemState);

            LocalTransformData.Update(ref systemState);

            TriggerVolumePortalData.Update(ref systemState);
            PhysicsVelocityData.Update(ref systemState);
            PhysicsGraphicalSmoothingData.Update(ref systemState);
        }

        public ComponentLookup<LocalToWorld> LocalToWorldData;

        public ComponentLookup<LocalTransform> LocalTransformData;

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

        public void Execute(Entity portalEntity, ref DynamicBuffer<StatefulTriggerEvent> triggerBuffer)
        {
            if (!ComponentDatas.TriggerVolumePortalData.HasComponent(portalEntity))
            {
                return;
            }

            var triggerVolumePortal = ComponentDatas.TriggerVolumePortalData[portalEntity];
            var companionEntity = triggerVolumePortal.Companion;
            var companionTriggerVolumePortal = ComponentDatas.TriggerVolumePortalData[companionEntity];

            using var processedEntities = new NativeHashSet<Entity>(256, Allocator.Temp);
            for (int i = 0; i < triggerBuffer.Length; i++)
            {
                var triggerEvent = triggerBuffer[i];
                var otherEntity = triggerEvent.GetOtherEntity(portalEntity);

                // check if we have already processed and potentially teleported this entity.
                // If yes, skip. This can occur if the entity contains a compound collider.
                if (!processedEntities.Add(otherEntity))
                {
                    continue;
                }

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

                    : new RigidTransform(ComponentDatas.LocalTransformData[portalEntity].Rotation, ComponentDatas.LocalTransformData[portalEntity].Position);

                var companionTransform = HierarchyChildMask.MatchesIgnoreFilter(companionEntity)
                    ? Math.DecomposeRigidBodyTransform(ComponentDatas.LocalToWorldData[companionEntity].Value)

                    : new RigidTransform(ComponentDatas.LocalTransformData[companionEntity].Rotation, ComponentDatas.LocalTransformData[companionEntity].Position);


                var portalPositionOffset = companionTransform.pos - portalTransform.pos;
                var portalRotationOffset = math.mul(companionTransform.rot, math.inverse(portalTransform.rot));


                var entityLocalTransformComponent = ComponentDatas.LocalTransformData[otherEntity];

                var entityVelocityComponent = ComponentDatas.PhysicsVelocityData[otherEntity];

                entityVelocityComponent.Linear = math.rotate(portalRotationOffset, entityVelocityComponent.Linear);

                entityLocalTransformComponent.Position += portalPositionOffset;
                entityLocalTransformComponent.Rotation = math.mul(entityLocalTransformComponent.Rotation, portalRotationOffset);

                ComponentDatas.LocalTransformData[otherEntity] = entityLocalTransformComponent;

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

            .WithAll<LocalTransform, PhysicsVelocity>()

            .WithNone<StatefulTriggerEvent>();
        m_NonTriggerDynamicBodyQuery = state.GetEntityQuery(builder);

        Assert.IsFalse(m_NonTriggerDynamicBodyQuery.HasFilter(), "The use of EntityQueryMask in this system will not respect the query's active filter settings.");
        m_NonTriggerDynamicBodyMask = m_NonTriggerDynamicBodyQuery.GetEntityQueryMask();

        _mComponentLookup = new TriggerVolumePortalComponentLookup(ref state);

        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadWrite<TriggerVolumePortal>()));
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
