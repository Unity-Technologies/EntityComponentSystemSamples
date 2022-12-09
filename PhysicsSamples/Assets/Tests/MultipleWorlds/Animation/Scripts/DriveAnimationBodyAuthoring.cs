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
            AddComponent(new DriveAnimationBodyData
            {
                DrivingEntity = GetEntity(authoring.DrivingEntity),
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
public partial class DriveAnimationBodySystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<DriveAnimationBodyData>();
    }

    protected override void OnUpdate()
    {
        var positions = GetComponentLookup<Translation>();
        var rotations = GetComponentLookup<Rotation>();
        var velocities = GetComponentLookup<PhysicsVelocity>();

        float stepFrequency = math.rcp(SystemAPI.Time.DeltaTime);
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
