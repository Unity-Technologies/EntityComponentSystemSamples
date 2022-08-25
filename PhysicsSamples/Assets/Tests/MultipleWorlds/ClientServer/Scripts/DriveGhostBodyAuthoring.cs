using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.Tests;
using Unity.Transforms;
using UnityEngine;

public class DriveGhostBodyAuthoring : MonoBehaviour//, IConvertGameObjectToEntity
{
    public ConvertToEntity DrivingEntity;
    [Tooltip("0 - Drive body by setting Velocity.\n1 - Drive body by setting Translation and Rotation.")]
    [Range(0, 1)] public float SetTransformGain;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new DriveGhostBodyData
        {
            DrivingEntity = conversionSystem.GetPrimaryEntity(DrivingEntity),
            FirstOrderGain = SetTransformGain
        });
    }
}

/// <summary>
/// Component that connects client and server Entities for server (ghost) bodies and specifies how sync will be done
/// </summary>
public struct DriveGhostBodyData : IComponentData
{
    public Entity DrivingEntity;

    /// <summary>
    /// Coefficient in range [0,1] denoting how much the client body will be driven by position (teleported), while the rest of position diff will be velocity-driven
    /// </summary>
    public float FirstOrderGain;
}

/// <summary>
/// System which syncs server bodies to the client
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ServerPhysicsSystem)), UpdateBefore(typeof(BuildPhysicsWorld))]
public partial class DriveGhostBodySystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate(GetEntityQuery(
            ComponentType.ReadOnly<DriveGhostBodyData>()
        ));
    }

    protected override void OnUpdate()
    {
        var positions = GetComponentDataFromEntity<Translation>();
        var rotations = GetComponentDataFromEntity<Rotation>();
        var velocities = GetComponentDataFromEntity<PhysicsVelocity>();

        float stepFrequency = math.rcp(Time.DeltaTime);
        Entities
            .WithBurst()
            .ForEach((Entity ghostEntity, in DriveGhostBodyData ghostBodyData, in PhysicsMass mass) =>
            {
                var serverRotation = rotations[ghostBodyData.DrivingEntity];
                var serverPosition = positions[ghostBodyData.DrivingEntity];

                var currentRotation = rotations[ghostEntity];
                var currentPosition = positions[ghostEntity];

                var desiredPosition = new Translation() { Value = currentPosition.Value };
                var desiredRotation = new Rotation() { Value = currentRotation.Value };

                // First order changes - position/rotation
                if (ghostBodyData.FirstOrderGain != 0)
                {
                    desiredPosition.Value = math.lerp(currentPosition.Value, serverPosition.Value, ghostBodyData.FirstOrderGain);
                    desiredRotation.Value = math.slerp(currentRotation.Value, serverRotation.Value, ghostBodyData.FirstOrderGain);
                    positions[ghostEntity] = desiredPosition;
                    rotations[ghostEntity] = desiredRotation;
                }

                // Second order changes - velocity
                if (ghostBodyData.FirstOrderGain != 1)
                {
                    var currentVelocity = velocities[ghostEntity];
                    var desiredVelocity = PhysicsVelocity.CalculateVelocityToTarget(
                        in mass, in desiredPosition, in desiredRotation,
                        new RigidTransform(serverRotation.Value, serverPosition.Value), stepFrequency);
                    velocities[ghostEntity] = desiredVelocity;
                }
            }).Schedule();
    }
}
