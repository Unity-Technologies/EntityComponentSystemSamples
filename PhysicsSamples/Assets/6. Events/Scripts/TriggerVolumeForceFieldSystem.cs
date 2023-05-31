using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Events
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct TriggerVolumeForceFieldSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TriggerVolumeForceField>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var nonTriggerDynamicBodyQuery = SystemAPI.QueryBuilder()
                .WithAllRW<PhysicsVelocity>()
                .WithAll<LocalTransform, PhysicsMass>()
                .WithNone<StatefulTriggerEvent>().Build();

            Assert.IsFalse(nonTriggerDynamicBodyQuery.HasFilter(),
                $"The use of EntityQueryMask in this system will not respect the query's active filter settings.");

            new ApplyForceFieldJob
            {
                NonTriggerDynamicBodyMask = nonTriggerDynamicBodyQuery.GetEntityQueryMask(),

                StepComponent = SystemAPI.HasSingleton<PhysicsStep>()
                    ? SystemAPI.GetSingleton<PhysicsStep>()
                    : PhysicsStep.Default,
                DeltaTime = SystemAPI.Time.DeltaTime,

                Masses = SystemAPI.GetComponentLookup<PhysicsMass>(),
                Velocities = SystemAPI.GetComponentLookup<PhysicsVelocity>(),
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>()
            }.Schedule();
        }

        [BurstCompile]
        partial struct ApplyForceFieldJob : IJobEntity
        {
            public EntityQueryMask NonTriggerDynamicBodyMask;
            public PhysicsStep StepComponent;
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransforms;
            [ReadOnly] public ComponentLookup<PhysicsMass> Masses;
            public ComponentLookup<PhysicsVelocity> Velocities;

            public void Execute(Entity e, ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer,
                ref TriggerVolumeForceField forceField)
            {
                forceField.Center = LocalTransforms[e].Position;

                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];
                    var otherEntity = triggerEvent.GetOtherEntity(e);

                    // exclude static bodies, other triggers and enter/exit events
                    if (triggerEvent.State != StatefulEventState.Stay ||
                        !NonTriggerDynamicBodyMask.MatchesIgnoreFilter(otherEntity))
                    {
                        continue;
                    }

                    var physicsVelocity = Velocities[otherEntity];
                    var physicsMass = Masses[otherEntity];
                    var pos = LocalTransforms[otherEntity];

                    ApplyForceField(DeltaTime, ref physicsVelocity, pos, physicsMass, forceField);

                    // counter-act gravity
                    physicsVelocity.Linear += -1.25f * StepComponent.Gravity * DeltaTime;

                    // write back
                    Velocities[otherEntity] = physicsVelocity;
                }
            }

            void ApplyForceField(
                in float dt,
                ref PhysicsVelocity bodyVelocity,
                in LocalTransform localTransform,
                in PhysicsMass bodyMass,
                in TriggerVolumeForceField forceField)
            {
                if (forceField.Strength == 0)
                    return;

                // Don't do anything if in eye
                float3 dir = float3.zero;

                dir = (forceField.Center - localTransform.Position);

                if (!math.any(dir))
                    return;

                // If force field around axis then project dir onto axis
                float3 axis = float3.zero;
                if (forceField.Axis != -1)
                {
                    axis[forceField.Axis] = 1f;
                    dir -= axis * math.dot(dir, axis);
                }

                float strength = forceField.Strength;
                float dist2 = math.lengthsq(dir);

                // Kill strength if in deadzone
                float dz2 = forceField.DeadZone * forceField.DeadZone;
                if (dz2 > dist2)
                    strength = 0;

                // If out of center and proportional divide by distance squared
                if (forceField.Proportional != 0)
                    strength = (dist2 > 1e-4f) ? strength / dist2 : 0;

                // Multiple through mass if want all objects moving equally
                dir = math.normalizesafe(dir);
                float mass = math.rcp(bodyMass.InverseMass);
                if (forceField.MassInvariant != 0) mass = 1f;
                strength *= mass * dt;
                bodyVelocity.Linear += strength * dir;

                // If want a rotational force field add extra twist deltas
                if ((forceField.Axis != -1) && (forceField.Rotation != 0))
                {
                    bodyVelocity.Linear += forceField.Rotation * strength * dir;
                    dir = math.cross(axis, -dir);
                    bodyVelocity.Linear += forceField.Rotation * strength * dir;
                }
            }
        }
    }
}
