using Unity.Assertions;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Events
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct TriggerVolumePortalSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TriggerVolumePortal>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hierarchyChildQuery = SystemAPI.QueryBuilder().WithAll<Parent, LocalToWorld>().Build();
            Assert.IsFalse(hierarchyChildQuery.HasFilter(),
                "The use of EntityQueryMask in this system will not respect the query's active filter settings.");

            var nonTriggerDynamicBodyQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, PhysicsVelocity>()
                .WithNone<StatefulTriggerEvent>().Build();
            Assert.IsFalse(nonTriggerDynamicBodyQuery.HasFilter(),
                "The use of EntityQueryMask in this system will not respect the query's active filter settings.");

            state.Dependency = new TriggerVolumePortalJob()
            {
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                TriggerVolumePortalLookup = SystemAPI.GetComponentLookup<TriggerVolumePortal>(),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(),
                PhysicsGraphicalSmoothingLookup = SystemAPI.GetComponentLookup<PhysicsGraphicalSmoothing>(),
                HierarchyChildMask = hierarchyChildQuery.GetEntityQueryMask(),
                NonTriggerDynamicBodyMask = nonTriggerDynamicBodyQuery.GetEntityQueryMask()
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        partial struct TriggerVolumePortalJob : IJobEntity
        {
            public EntityQueryMask HierarchyChildMask;
            public EntityQueryMask NonTriggerDynamicBodyMask;
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            public ComponentLookup<TriggerVolumePortal> TriggerVolumePortalLookup;
            public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
            public ComponentLookup<PhysicsGraphicalSmoothing> PhysicsGraphicalSmoothingLookup;

            public void Execute(Entity portalEntity, ref DynamicBuffer<StatefulTriggerEvent> triggerBuffer)
            {
                if (!TriggerVolumePortalLookup.HasComponent(portalEntity))
                {
                    return;
                }

                var triggerVolumePortal = TriggerVolumePortalLookup[portalEntity];
                var companionEntity = triggerVolumePortal.Companion;
                var companionTriggerVolumePortal = TriggerVolumePortalLookup[companionEntity];

                for (int i = 0; i < triggerBuffer.Length; i++)
                {
                    var triggerEvent = triggerBuffer[i];
                    var otherEntity = triggerEvent.GetOtherEntity(portalEntity);

                    // exclude other triggers, static bodies and processed events
                    if (triggerEvent.State != StatefulEventState.Enter ||
                        !NonTriggerDynamicBodyMask.MatchesIgnoreFilter(otherEntity))
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
                        ? Math.DecomposeRigidBodyTransform(LocalToWorldLookup[portalEntity].Value)
                        : new RigidTransform(LocalTransformLookup[portalEntity].Rotation,
                        LocalTransformLookup[portalEntity].Position);

                    var companionTransform = HierarchyChildMask.MatchesIgnoreFilter(companionEntity)
                        ? Math.DecomposeRigidBodyTransform(LocalToWorldLookup[companionEntity].Value)
                        : new RigidTransform(LocalTransformLookup[companionEntity].Rotation,
                        LocalTransformLookup[companionEntity].Position);


                    var portalPositionOffset = companionTransform.pos - portalTransform.pos;
                    var portalRotationOffset = math.mul(companionTransform.rot, math.inverse(portalTransform.rot));

                    var entityLocalTransformComponent = LocalTransformLookup[otherEntity];
                    var entityVelocityComponent = PhysicsVelocityLookup[otherEntity];

                    entityVelocityComponent.Linear = math.rotate(portalRotationOffset, entityVelocityComponent.Linear);
                    entityLocalTransformComponent.Position += portalPositionOffset;
                    entityLocalTransformComponent.Rotation =
                        math.mul(entityLocalTransformComponent.Rotation, portalRotationOffset);

                    LocalTransformLookup[otherEntity] = entityLocalTransformComponent;
                    PhysicsVelocityLookup[otherEntity] = entityVelocityComponent;

                    if (PhysicsGraphicalSmoothingLookup.HasComponent(otherEntity))
                    {
                        var entitySmoothingComponent = PhysicsGraphicalSmoothingLookup[otherEntity];
                        entitySmoothingComponent.ApplySmoothing = 0;
                        PhysicsGraphicalSmoothingLookup[otherEntity] = entitySmoothingComponent;
                    }

                    companionTriggerVolumePortal.TransferCount++;
                }

                TriggerVolumePortalLookup[portalEntity] = triggerVolumePortal;
                TriggerVolumePortalLookup[companionEntity] = companionTriggerVolumePortal;
            }
        }
    }
}
