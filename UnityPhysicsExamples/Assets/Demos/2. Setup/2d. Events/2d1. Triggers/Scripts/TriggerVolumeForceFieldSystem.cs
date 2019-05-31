using Unity.Physics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Transforms;
using Unity.Burst;

[UpdateBefore(typeof(ForceFieldSystem))]//, UpdateBefore(typeof(EndFramePhysicsSystem))]
public class TriggerVolumeForceFieldSystem : JobComponentSystem
{
    public EntityQuery m_OverlappingGroup;

    protected override void OnCreate()
    {
        m_OverlappingGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(OverlappingTriggerVolume),
                typeof(ForceFieldOverlappingTriggerVolume),
            }
        });
    }

    [BurstCompile]
    struct ForceFieldOverlapUpdateJob : IJob
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> OverlappingEntities;
        [ReadOnly] public PhysicsStep StepComponent;
        public float DeltaTime;

        [ReadOnly] public ComponentDataFromEntity<OverlappingTriggerVolume> OverlappingComponents;
        [ReadOnly] public ComponentDataFromEntity<TriggerVolume> TriggerComponents;
        [ReadOnly] public ComponentDataFromEntity<ForceField> ForceFieldComponents;
        [ReadOnly] public ComponentDataFromEntity<Translation> PositionComponents;
        [ReadOnly] public ComponentDataFromEntity<Rotation> RotationComponents;
        [ReadOnly] public ComponentDataFromEntity<PhysicsMass> MassComponents;
        public ComponentDataFromEntity<PhysicsVelocity> VelocityComponents;

        public void Execute()
        {
            for (int i = 0; i < OverlappingEntities.Length; i++)
            {
                var overlappingEntity = OverlappingEntities[i];
                if (!MassComponents.Exists(overlappingEntity))
                {
                    continue;
                }

                var overlapComponent = OverlappingComponents[overlappingEntity];
                var volumeEntity = overlapComponent.VolumeEntity;

                var position = PositionComponents[overlappingEntity];
                var rotation = RotationComponents[overlappingEntity];
                var mass = MassComponents[overlappingEntity];
                var velocity = VelocityComponents[overlappingEntity];
                var forceField = ForceFieldComponents[volumeEntity];

                // Set the force field center to the position of the 
                // tornado trigger volume and enable it
                forceField.center = PositionComponents[volumeEntity].Value;
                forceField.enabled = 1;

                // Directly call the ForceFieldJob execute function to apply the velocity change
                var forceFieldJob = new ForceFieldSystem.ForceFieldJob() { dt = DeltaTime };
                forceFieldJob.Execute(ref position, ref rotation, ref mass, ref velocity, ref forceField);

                // Counter-act gravity
                velocity.Linear += -1.25f * StepComponent.Gravity * DeltaTime;

                VelocityComponents[overlappingEntity] = velocity;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var enteredEntities = m_OverlappingGroup.ToEntityArray(Allocator.TempJob);
        var stepComponent = GetSingleton<PhysicsStep>();

        var overlappingComponents = GetComponentDataFromEntity<OverlappingTriggerVolume>(true);
        var triggerComponents = GetComponentDataFromEntity<TriggerVolume>(true);
        var forcefieldComponents = GetComponentDataFromEntity<ForceField>(true);
        var positionComponents = GetComponentDataFromEntity<Translation>(true);
        var rotationComponents = GetComponentDataFromEntity<Rotation>(true);
        var massComponents = GetComponentDataFromEntity<PhysicsMass>(true);
        var velocityComponents = GetComponentDataFromEntity<PhysicsVelocity>();

        JobHandle job = new ForceFieldOverlapUpdateJob()
        {
            OverlappingEntities = enteredEntities,
            StepComponent = stepComponent,
            DeltaTime = Time.fixedDeltaTime,

            OverlappingComponents = overlappingComponents,
            TriggerComponents = triggerComponents,
            ForceFieldComponents = forcefieldComponents,
            PositionComponents = positionComponents,
            RotationComponents = rotationComponents,
            MassComponents = massComponents,

            VelocityComponents = velocityComponents,
        }.Schedule(inputDeps);

        return job;
    }
}



