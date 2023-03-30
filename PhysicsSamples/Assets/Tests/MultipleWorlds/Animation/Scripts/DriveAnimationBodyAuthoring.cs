using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.Tests;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

public class DriveAnimationBodyAuthoring : MonoBehaviour
{
    public GameObject DrivingEntity;
    [Range(0f, 1f)] public float PositionGain;
    [Range(0f, 1f)] public float RotationGain;
    [Range(0f, 1f)] public float LinearVelocityGain;
    [Range(0f, 1f)] public float AngularVelocityGain;

    protected void OnEnable() {}

    class DriveAnimationBodyBaker : Baker<DriveAnimationBodyAuthoring>
    {
        public override void Bake(DriveAnimationBodyAuthoring authoring)
        {
            var worldFromDriven = new RigidTransform(authoring.transform.rotation, authoring.transform.position);
            var worldFromDriving = new RigidTransform(authoring.DrivingEntity.transform.rotation, authoring.DrivingEntity.transform.position);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DriveAnimationBodyData
            {
                DrivingEntity = GetEntity(authoring.DrivingEntity, TransformUsageFlags.Dynamic),
                DrivingFromDriven = math.mul(math.inverse(worldFromDriving), worldFromDriven),
                PositionGain = authoring.PositionGain,
                RotationGain = authoring.RotationGain,
                LinearVelocityGain = authoring.LinearVelocityGain,
                AngularVelocityGain = authoring.AngularVelocityGain
            });
        }
    }
}

public struct DriveAnimationBodyData : IComponentData
{
    public Entity DrivingEntity;
    public RigidTransform DrivingFromDriven;
    public float PositionGain;
    public float RotationGain;
    public float LinearVelocityGain;
    public float AngularVelocityGain;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(AnimationPhysicsSystem))]
public partial struct DriveAnimationBodySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DriveAnimationBodyData>();
    }

    [BurstCompile]
    public partial struct DriveAnimationBodyJob : IJobEntity
    {
        public ComponentLookup<LocalTransform> LocalTransforms;
        public ComponentLookup<PhysicsVelocity> Velocities;
        public float StepFrequency;

        [BurstCompile]
        public void Execute(Entity drivenEntity, in DriveAnimationBodyData driveData, in PhysicsMass mass)
        {
            var drivingLocalTransform = LocalTransforms[driveData.DrivingEntity];
            var currentLocalTransform = LocalTransforms[drivenEntity];

            // First order gains - position/rotation
            var worldFromDriving = new RigidTransform(drivingLocalTransform.Rotation, drivingLocalTransform.Position);
            var worldFromDriven = math.mul(worldFromDriving, driveData.DrivingFromDriven);

            var desiredLocalTransform = LocalTransform.FromPositionRotationScale(
                math.lerp(currentLocalTransform.Position, worldFromDriven.pos, driveData.PositionGain),
                math.slerp(currentLocalTransform.Rotation, worldFromDriven.rot, driveData.RotationGain),
                LocalTransforms[drivenEntity].Scale);
            LocalTransforms[drivenEntity] = desiredLocalTransform;

            // Second order gains - velocity
            var currentVelocity = Velocities[drivenEntity];
            var desiredVelocity = PhysicsVelocity.CalculateVelocityToTarget(
                in mass, in desiredLocalTransform.Position, in desiredLocalTransform.Rotation,
                worldFromDriven, StepFrequency);
            desiredVelocity.Linear = math.lerp(currentVelocity.Linear, desiredVelocity.Linear, driveData.LinearVelocityGain);
            desiredVelocity.Angular = math.lerp(currentVelocity.Angular, desiredVelocity.Angular, driveData.AngularVelocityGain);
            Velocities[drivenEntity] = desiredVelocity;
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var localTransforms = SystemAPI.GetComponentLookup<LocalTransform>();
        var velocities = SystemAPI.GetComponentLookup<PhysicsVelocity>();

        float stepFrequency = math.rcp(SystemAPI.Time.DeltaTime);

        new DriveAnimationBodyJob
        {
            LocalTransforms = localTransforms,
            Velocities = velocities,
            StepFrequency = stepFrequency
        }.Schedule();
    }
}
