using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct TriggerVolumeForceField : IComponentData
{
    public float3 Center;
    public float DeadZone;
    public float Strength;
    public float Rotation;
    public int Axis;
    public int Proportional;
    public int MassInvariant;
}

public class TriggerVolumeForceFieldAuthoring : MonoBehaviour
{
    public enum Direction { Center, XAxis, YAxis, ZAxis };

    public float Strength = 10f;
    public float DeadZone = 0.5f;
    public Direction Axis = Direction.Center;
    public float Rotation = 0;
    public bool Proportional = true;
    public bool MassInvariant = false;
}

class TriggerVolumeForceFieldAuthoringBaker : Baker<TriggerVolumeForceFieldAuthoring>
{
    public override void Bake(TriggerVolumeForceFieldAuthoring authoring)
    {
        var transform = GetComponent<Transform>();
        AddComponent(new TriggerVolumeForceField
        {
            Center = transform.position,
            Strength = authoring.Strength,
            DeadZone = (authoring.DeadZone == 0) ? 0.001f : math.abs(authoring.DeadZone),
            Axis = (int)authoring.Axis - 1,
            Rotation = math.radians(authoring.Rotation),
            Proportional = authoring.Proportional ? 1 : 0,
            MassInvariant = authoring.MassInvariant ? 1 : 0
        });
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct TriggerVolumeForceFieldSystem : ISystem
{
    private EntityQuery m_NonTriggerDynamicBodyQuery;
    private EntityQueryMask m_NonTriggerDynamicBodyMask;
    private ComponentDataHandles m_Handles;

    struct ComponentDataHandles
    {
#if !ENABLE_TRANSFORM_V1
        public ComponentLookup<LocalTransform> LocalTransformFromEntity;
#else
        public ComponentLookup<Translation> TranslationFromEntity;
#endif
        public ComponentLookup<PhysicsMass> MassFromEntity;
        public ComponentLookup<PhysicsVelocity> VelocityFromEntity;

        public ComponentDataHandles(ref SystemState state)
        {
#if !ENABLE_TRANSFORM_V1
            LocalTransformFromEntity = state.GetComponentLookup<LocalTransform>(true);
#else
            TranslationFromEntity = state.GetComponentLookup<Translation>(true);
#endif
            MassFromEntity = state.GetComponentLookup<PhysicsMass>(true);
            VelocityFromEntity = state.GetComponentLookup<PhysicsVelocity>(false);
        }

        public void Update(ref SystemState state)
        {
#if !ENABLE_TRANSFORM_V1
            LocalTransformFromEntity.Update(ref state);
#else
            TranslationFromEntity.Update(ref state);
#endif
            MassFromEntity.Update(ref state);
            VelocityFromEntity.Update(ref state);
        }
    }

    public static void ApplyForceField(
        in float dt,
        ref PhysicsVelocity bodyVelocity,
#if !ENABLE_TRANSFORM_V1
        in LocalTransform localTransform, in PhysicsMass bodyMass, in TriggerVolumeForceField forceField
#else
        in Translation pos, in PhysicsMass bodyMass, in TriggerVolumeForceField forceField
#endif
    )
    {
        if (forceField.Strength == 0)
            return;

        // Don't do anything if in eye
        float3 dir = float3.zero;
#if !ENABLE_TRANSFORM_V1
        dir = (forceField.Center - localTransform.Position);
#else
        dir = (forceField.Center - pos.Value);
#endif
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

    [BurstCompile]
    partial struct ApplyForceFieldJob : IJobEntity
    {
        public EntityQueryMask NonTriggerDynamicBodyMask;

        public PhysicsStep StepComponent;
        public float DeltaTime;

#if !ENABLE_TRANSFORM_V1
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransforms;
#else
        [ReadOnly] public ComponentLookup<Translation> Translations;
#endif
        [ReadOnly] public ComponentLookup<PhysicsMass> Masses;
        public ComponentLookup<PhysicsVelocity> Velocities;

        [BurstCompile]
        public void Execute(Entity e, ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer, ref TriggerVolumeForceField forceField)
        {
#if !ENABLE_TRANSFORM_V1
            forceField.Center = LocalTransforms[e].Position;
#else
            forceField.Center = Translations[e].Value;
#endif

            for (int i = 0; i < triggerEventBuffer.Length; i++)
            {
                var triggerEvent = triggerEventBuffer[i];

                var otherEntity = triggerEvent.GetOtherEntity(e);

                // exclude static bodies, other triggers and enter/exit events
                if (triggerEvent.State != StatefulEventState.Stay || !NonTriggerDynamicBodyMask.MatchesIgnoreFilter(otherEntity))
                {
                    continue;
                }

                var physicsVelocity = Velocities[otherEntity];
                var physicsMass = Masses[otherEntity];
#if !ENABLE_TRANSFORM_V1
                var pos = LocalTransforms[otherEntity];
#else
                var pos = Translations[otherEntity];
#endif

                ApplyForceField(DeltaTime, ref physicsVelocity, pos, physicsMass, forceField);

                // counter-act gravity
                physicsVelocity.Linear += -1.25f * StepComponent.Gravity * DeltaTime;

                // write back
                Velocities[otherEntity] = physicsVelocity;
            }
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
            .WithAllRW<PhysicsVelocity>()
#if !ENABLE_TRANSFORM_V1
            .WithAll<LocalTransform, PhysicsMass>()
#else
            .WithAll<Translation, PhysicsMass>()
#endif
            .WithNone<StatefulTriggerEvent>();
        m_NonTriggerDynamicBodyQuery = state.GetEntityQuery(builder);

        Assert.IsFalse(m_NonTriggerDynamicBodyQuery.HasFilter(), "The use of EntityQueryMask in this system will not respect the query's active filter settings.");
        m_NonTriggerDynamicBodyMask = m_NonTriggerDynamicBodyQuery.GetEntityQueryMask();

        state.RequireForUpdate<TriggerVolumeForceField>();
        m_Handles = new ComponentDataHandles(ref state);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_Handles.Update(ref state);
        var stepComponent = SystemAPI.HasSingleton<PhysicsStep>() ? SystemAPI.GetSingleton<PhysicsStep>() : PhysicsStep.Default;
        // TODO(DOTS-6141): This expression can't currently be inlined into the IJobEntity initializer
        float dt = SystemAPI.Time.DeltaTime;
        var applyForceFieldJob = new ApplyForceFieldJob
        {
            NonTriggerDynamicBodyMask = m_NonTriggerDynamicBodyMask,

            StepComponent = stepComponent,
            DeltaTime = dt,


            Masses = m_Handles.MassFromEntity,
            Velocities = m_Handles.VelocityFromEntity
        };

#if !ENABLE_TRANSFORM_V1
        applyForceFieldJob.LocalTransforms = m_Handles.LocalTransformFromEntity;
#else
        applyForceFieldJob.Translations = m_Handles.TranslationFromEntity;
#endif

        state.Dependency = applyForceFieldJob.Schedule(state.Dependency);
    }
}
