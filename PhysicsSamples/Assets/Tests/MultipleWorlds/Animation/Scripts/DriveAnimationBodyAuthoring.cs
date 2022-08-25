using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.Tests;
using Unity.Transforms;
using UnityEngine;

public class DriveAnimationBodyAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public ConvertToEntity DrivingEntity;
    [Range(0f, 1f)] public float PositionGain;
    [Range(0f, 1f)] public float RotationGain;
    [Range(0f, 1f)] public float LinearVelocityGain;
    [Range(0f, 1f)] public float AngularVelocityGain;

    protected void OnEnable() {}

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var worldFromDriven = new RigidTransform(transform.rotation, transform.position);
        var worldFromDriving = new RigidTransform(DrivingEntity.transform.rotation, DrivingEntity.transform.position);
        dstManager.AddComponentData<DriveAnimationBodyData>(entity, new DriveAnimationBodyData
        {
            DrivingEntity = conversionSystem.GetPrimaryEntity(DrivingEntity),
            DrivingFromDriven = math.mul(math.inverse(worldFromDriving), worldFromDriven),
            PositionGain = PositionGain,
            RotationGain = RotationGain,
            LinearVelocityGain = LinearVelocityGain,
            AngularVelocityGain = AngularVelocityGain
        });
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
[UpdateAfter(typeof(EndFramePhysicsSystem))]
[UpdateBefore(typeof(AnimationPhysicsSystem))]
public partial class DriveAnimationBodySystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate(GetEntityQuery(
            ComponentType.ReadOnly<DriveAnimationBodyData>()
        ));
    }

    protected override void OnUpdate()
    {
        var positions = GetComponentDataFromEntity<Translation>();
        var rotations = GetComponentDataFromEntity<Rotation>();
        var velocities = GetComponentDataFromEntity<PhysicsVelocity>();

        float stepFrequency = math.rcp(Time.DeltaTime);
        Entities.
            WithBurst().
            ForEach((Entity drivenEntity, in DriveAnimationBodyData driveData, in PhysicsMass mass) =>
            {
                var drivingRotation = rotations[driveData.DrivingEntity];
                var drivingPosition = positions[driveData.DrivingEntity];

                var currentRotation = rotations[drivenEntity];
                var currentPosition = positions[drivenEntity];

                // First order gains - position/rotation
                var worldFromDriving = new RigidTransform(drivingRotation.Value, drivingPosition.Value);
                var worldFromDriven = math.mul(worldFromDriving, driveData.DrivingFromDriven);

                var desiredPosition = new Translation() { Value = math.lerp(currentPosition.Value, worldFromDriven.pos, driveData.PositionGain) };
                var desiredRotation = new Rotation() { Value = math.slerp(currentRotation.Value, worldFromDriven.rot, driveData.RotationGain) };
                positions[drivenEntity] = desiredPosition;
                rotations[drivenEntity] = desiredRotation;

                // Second order gains - velocity
                var currentVelocity = velocities[drivenEntity];
                var desiredVelocity = PhysicsVelocity.CalculateVelocityToTarget(
                    in mass, in desiredPosition, in desiredRotation,
                    worldFromDriven, stepFrequency);
                desiredVelocity.Linear = math.lerp(currentVelocity.Linear, desiredVelocity.Linear, driveData.LinearVelocityGain);
                desiredVelocity.Angular = math.lerp(currentVelocity.Angular, desiredVelocity.Angular, driveData.AngularVelocityGain);
                velocities[drivenEntity] = desiredVelocity;
            }).Schedule();
    }
}
