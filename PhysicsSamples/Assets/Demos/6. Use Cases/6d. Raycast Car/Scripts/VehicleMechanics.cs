using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace Demos
{
    [RequireComponent(typeof(PhysicsBodyAuthoring))]
    public class VehicleMechanics : MonoBehaviour
    {
        [Header("Wheel Parameters...")]
        public List<GameObject> wheels = new List<GameObject>();
        public float wheelBase = 0.5f;
        public float wheelFrictionRight = 0.5f;
        public float wheelFrictionForward = 0.5f;
        public float wheelMaxImpulseRight = 10.0f;
        public float wheelMaxImpulseForward = 10.0f;
        [Header("Suspension Parameters...")]
        public float suspensionLength = 0.5f;
        public float suspensionStrength = 1.0f;
        public float suspensionDamping = 0.1f;
        [Header("Steering Parameters...")]
        public List<GameObject> steeringWheels = new List<GameObject>();
        [Header("Drive Parameters...")]
        public List<GameObject> driveWheels = new List<GameObject>();
        [Header("Miscellaneous Parameters...")]
        public bool drawDebugInformation = false;
    }

    struct WheelBakingInfo
    {
        public Entity Wheel;
        public Entity GraphicalRepresentation;
        public RigidTransform WorldFromSuspension;
        public RigidTransform WorldFromChassis;
    }

    [TemporaryBakingType]
    struct VehicleMechanicsForBaking : IComponentData
    {
        public NativeArray<WheelBakingInfo> Wheels;
        public NativeArray<Entity> steeringWheels;
        public NativeArray<Entity> driveWheels;
    }

    partial class VehicleMechanicsBaker : Baker<VehicleMechanics>
    {
        public override void Bake(VehicleMechanics authoring)
        {
            AddComponent<VehicleBody>();
            AddComponent(new VehicleConfiguration
            {
                wheelBase = authoring.wheelBase,
                wheelFrictionRight = authoring.wheelFrictionRight,
                wheelFrictionForward = authoring.wheelFrictionForward,
                wheelMaxImpulseRight = authoring.wheelMaxImpulseRight,
                wheelMaxImpulseForward = authoring.wheelMaxImpulseForward,
                suspensionLength = authoring.suspensionLength,
                suspensionStrength = authoring.suspensionStrength,
                suspensionDamping = authoring.suspensionDamping,
                invWheelCount = 1f / authoring.wheels.Count,
                drawDebugInformation = (byte)(authoring.drawDebugInformation ? 1 : 0)
            });
            AddComponent(new VehicleMechanicsForBaking()
            {
                Wheels = GetWheelInfo(authoring.wheels, Allocator.Temp),
                steeringWheels = ToNativeArray(authoring.steeringWheels, Allocator.Temp),
                driveWheels = ToNativeArray(authoring.driveWheels, Allocator.Temp)
            });
        }

        NativeArray<WheelBakingInfo> GetWheelInfo(List<GameObject> wheels, Allocator allocator)
        {
            if (wheels == null)
                return default;

            var array = new NativeArray<WheelBakingInfo>(wheels.Count, allocator);
            int i = 0;
            foreach (var wheel in wheels)
            {
                RigidTransform worldFromSuspension = new RigidTransform
                {
                    pos = wheel.transform.parent.position,
                    rot = wheel.transform.parent.rotation
                };

                RigidTransform worldFromChassis = new RigidTransform
                {
                    pos = wheel.transform.parent.parent.parent.position,
                    rot = wheel.transform.parent.parent.parent.rotation
                };

                array[i++] = new WheelBakingInfo()
                {
                    Wheel = GetEntity(wheel),
                    GraphicalRepresentation = GetEntity(wheel.transform.GetChild(0)),
                    WorldFromSuspension = worldFromSuspension,
                    WorldFromChassis = worldFromChassis,
                };
            }

            return array;
        }

        NativeArray<Entity> ToNativeArray(List<GameObject> list, Allocator allocator)
        {
            if (list == null)
                return default;

            var array = new NativeArray<Entity>(list.Count, allocator);
            for (int i = 0; i < list.Count; ++i)
                array[i] = GetEntity(list[i]);

            return array;
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(EndColliderBakingSystem))]
    [UpdateAfter(typeof(PhysicsBodyBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class VehicleMechanicsBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var(m, vehicleEntity)
                     in SystemAPI.Query<RefRO<VehicleMechanicsForBaking>>().WithEntityAccess().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                foreach (var wheel in m.ValueRO.Wheels)
                {
                    var wheelEntity = wheel.Wheel;

                    // Assumed hierarchy:
                    // - chassis
                    //  - mechanics
                    //   - suspension
                    //    - wheel (rotates about yaw axis and translates along suspension up)
                    //     - graphic (rotates about pitch axis)

                    RigidTransform worldFromSuspension = wheel.WorldFromSuspension;

                    RigidTransform worldFromChassis = wheel.WorldFromChassis;

                    var chassisFromSuspension = math.mul(math.inverse(worldFromChassis), worldFromSuspension);

                    commandBuffer.AddComponent(wheelEntity, new Wheel
                    {
                        Vehicle = vehicleEntity,
                        GraphicalRepresentation = wheel.GraphicalRepresentation, // assume wheel has a single child with rotating graphic
                        // TODO assume for now that driving/steering wheels also appear in this list
                        UsedForSteering = (byte)(m.ValueRO.steeringWheels.Contains(wheelEntity) ? 1 : 0),
                        UsedForDriving = (byte)(m.ValueRO.driveWheels.Contains(wheelEntity) ? 1 : 0),
                        ChassisFromSuspension = chassisFromSuspension
                    });
                }
            }
            commandBuffer.Playback(EntityManager);
        }
    }

    // configuration properties of the vehicle mechanics, which change with low frequency at run-time
    struct VehicleConfiguration : IComponentData
    {
        public float wheelBase;
        public float wheelFrictionRight;
        public float wheelFrictionForward;
        public float wheelMaxImpulseRight;
        public float wheelMaxImpulseForward;
        public float suspensionLength;
        public float suspensionStrength;
        public float suspensionDamping;
        public float invWheelCount;
        public byte drawDebugInformation;
    }

    // physics properties of the vehicle rigid body, which change with high frequency at run-time
    struct VehicleBody : IComponentData
    {
        public float SlopeSlipFactor;
        public float3 WorldCenterOfMass;
    }

    struct Wheel : IComponentData
    {
        public Entity Vehicle;
        public Entity GraphicalRepresentation;
        public byte UsedForSteering;
        public byte UsedForDriving;
        public RigidTransform ChassisFromSuspension;
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsInitializeGroup)), UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class VehicleMechanicsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<VehicleConfiguration>();
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            // update vehicle properties first
            Dependency = Entities
                .WithName("PrepareVehiclesJob")
                .WithBurst()
                .ForEach((
                    Entity entity, ref VehicleBody vehicleBody,
#if !ENABLE_TRANSFORM_V1
                    in VehicleConfiguration mechanics, in PhysicsMass mass, in LocalTransform localTransform
                    ) =>
                    {
                        vehicleBody.WorldCenterOfMass = mass.GetCenterOfMassWorldSpace(localTransform.Position, localTransform.Rotation);

                        // calculate a simple slip factor based on chassis tilt
                        float3 worldUp = math.mul(localTransform.Rotation, math.up());
#else
                    in VehicleConfiguration mechanics, in PhysicsMass mass, in Translation translation, in Rotation rotation
                    ) =>
                    {
                        vehicleBody.WorldCenterOfMass = mass.GetCenterOfMassWorldSpace(in translation.Value, in rotation.Value);

                        // calculate a simple slip factor based on chassis tilt
                        float3 worldUp = math.mul(rotation.Value, math.up());
#endif
                        vehicleBody.SlopeSlipFactor = math.pow(math.abs(math.dot(worldUp, math.up())), 4f);
                    })
                .Schedule(Dependency);

            Dependency.Complete();

            // this sample makes direct modifications to impulses between PhysicsInitializeWorldGroup and StepPhysicsSimulationGroup
            // we thus use PhysicsWorldExtensions rather than modifying component data, since they have already been consumed by BuildPhysicsWorld
            PhysicsWorld world = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;
            EntityManager.CompleteDependencyBeforeRW<PhysicsWorldSingleton>();

            // update each wheel
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

#if !ENABLE_TRANSFORM_V1
            foreach (var(localTransform, wheel, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Wheel>>().WithEntityAccess())
            {
                var newLocalTransform = localTransform;
#else
            foreach (var(localPosition, localRotation, wheel, entity) in SystemAPI.Query<RefRW<Translation>, RefRW<Rotation>, RefRO<Wheel>>().WithEntityAccess())
            {
#endif
                Entity ce = wheel.ValueRO.Vehicle;
                if (ce == Entity.Null) return;
                int ceIdx = world.GetRigidBodyIndex(ce);
                if (-1 == ceIdx || ceIdx >= world.NumDynamicBodies) return;

                var mechanics = SystemAPI.GetComponent<VehicleConfiguration>(ce);
                var vehicleBody = SystemAPI.GetComponent<VehicleBody>(ce);

#if !ENABLE_TRANSFORM_V1
                var t = SystemAPI.GetComponent<LocalTransform>(ce);
                float3 cePosition = t.Position;
                quaternion ceRotation = t.Rotation;
#else
                float3 cePosition = GetComponent<Translation>(ce).Value;
                quaternion ceRotation = GetComponent<Rotation>(ce).Value;
#endif
                float3 ceCenterOfMass = vehicleBody.WorldCenterOfMass;
                float3 ceUp = math.mul(ceRotation, new float3(0f, 1f, 0f));
                float3 ceForward = math.mul(ceRotation, new float3(0f, 0f, 1f));
                float3 ceRight = math.mul(ceRotation, new float3(1f, 0f, 0f));

                CollisionFilter filter = world.GetCollisionFilter(ceIdx);

                float driveDesiredSpeed = 0f;
                bool driveEngaged = false;
                if (SystemAPI.HasComponent<VehicleSpeed>(ce))
                {
                    var vehicleSpeed = SystemAPI.GetComponent<VehicleSpeed>(ce);
                    driveDesiredSpeed = vehicleSpeed.DesiredSpeed;
                    driveEngaged = vehicleSpeed.DriveEngaged != 0;
                }

                float desiredSteeringAngle = SystemAPI.HasComponent<VehicleSteering>(ce)
                    ? SystemAPI.GetComponent<VehicleSteering>(ce).DesiredSteeringAngle
                    : 0f;

                RigidTransform worldFromChassis = new RigidTransform
                {
                    pos = cePosition,
                    rot = ceRotation
                };

                RigidTransform suspensionFromWheel = new RigidTransform
                {
#if !ENABLE_TRANSFORM_V1
                    pos = localTransform.ValueRO.Position,
                    rot = localTransform.ValueRO.Rotation
#else
                    pos = localPosition.ValueRO.Value,
                    rot = localRotation.ValueRO.Value
#endif
                };

                RigidTransform chassisFromWheel = math.mul(wheel.ValueRO.ChassisFromSuspension, suspensionFromWheel);
                RigidTransform worldFromLocal = math.mul(worldFromChassis, chassisFromWheel);

                // create a raycast from the suspension point on the chassis
                var worldFromSuspension = math.mul(worldFromChassis, wheel.ValueRO.ChassisFromSuspension);
                float3 rayStart = worldFromSuspension.pos;
                float3 rayEnd = (-ceUp * (mechanics.suspensionLength + mechanics.wheelBase)) + rayStart;

                if (mechanics.drawDebugInformation != 0)
                    Debug.DrawRay(rayStart, rayEnd - rayStart);

                var raycastInput = new RaycastInput
                {
                    Start = rayStart,
                    End = rayEnd,
                    Filter = filter
                };

                var hit = world.CastRay(raycastInput, out var rayResult);

                var invWheelCount = mechanics.invWheelCount;

                // Calculate a simple slip factor based on chassis tilt.
                float slopeSlipFactor = vehicleBody.SlopeSlipFactor;

                float3 wheelPos = math.select(raycastInput.End, rayResult.Position, hit);
                wheelPos -= (cePosition - ceCenterOfMass);

                float3 velocityAtWheel = world.GetLinearVelocity(ceIdx, wheelPos);

                float3 weUp = ceUp;
                float3 weRight = ceRight;
                float3 weForward = ceForward;

                // Assumed hierarchy:
                // - chassis
                //  - mechanics
                //   - suspension
                //    - wheel (rotates about yaw axis and translates along suspension up)
                //     - graphic (rotates about pitch axis)

                #region handle wheel steering
                {
                    // update yaw angle if wheel is used for steering
                    if (wheel.ValueRO.UsedForSteering != 0)
                    {
                        quaternion wRotation = quaternion.AxisAngle(ceUp, desiredSteeringAngle);
                        weRight = math.rotate(wRotation, weRight);
                        weForward = math.rotate(wRotation, weForward);

#if !ENABLE_TRANSFORM_V1
                        newLocalTransform.ValueRW.Rotation = quaternion.AxisAngle(math.up(), desiredSteeringAngle);
#else
                        commandBuffer.SetComponent(entity, new Rotation { Value = quaternion.AxisAngle(math.up(), desiredSteeringAngle) });
#endif
                    }
                }
                #endregion

                float currentSpeedUp = math.dot(velocityAtWheel, weUp);
                float currentSpeedForward = math.dot(velocityAtWheel, weForward);
                float currentSpeedRight = math.dot(velocityAtWheel, weRight);

                #region handle wheel rotation
                {
                    // update rotation of graphical representation about axle
                    bool isDriven = driveEngaged && wheel.ValueRO.UsedForDriving != 0;
                    float weRotation = isDriven
                        ? (driveDesiredSpeed / mechanics.wheelBase)
                        : (currentSpeedForward / mechanics.wheelBase);

                    weRotation = math.radians(weRotation);
#if !ENABLE_TRANSFORM_V1
                    newLocalTransform.ValueRW.Rotation = math.mul(localTransform.ValueRO.Rotation, quaternion.AxisAngle(new float3(1f, 0f, 0f), weRotation));         // TODO Should this use newLocalTransform to read from?
#else
                    var currentRotation = SystemAPI.GetComponent<Rotation>(wheel.ValueRO.GraphicalRepresentation).Value;
                    commandBuffer.SetComponent(wheel.ValueRO.GraphicalRepresentation, new Rotation
                    {
                        // assumes wheels are aligned with chassis in "bind pose"
                        Value = math.mul(currentRotation, quaternion.AxisAngle(new float3(1f, 0f, 0f), weRotation))
                    });
#endif
                }
                #endregion

                var parentFromWorld = math.inverse(worldFromSuspension);
                if (!hit)
                {
                    float3 wheelDesiredPos = (-ceUp * mechanics.suspensionLength) + rayStart;
                    var worldPosition = math.lerp(worldFromLocal.pos, wheelDesiredPos, mechanics.suspensionDamping / mechanics.suspensionStrength);
                    // update translation of wheels along suspension column
#if !ENABLE_TRANSFORM_V1
                    newLocalTransform.ValueRW.Position = math.mul(parentFromWorld, new float4(worldPosition, 1f)).xyz;
#else
                    commandBuffer.SetComponent(entity, new Translation
                    {
                        Value = math.mul(parentFromWorld, new float4(worldPosition, 1f)).xyz
                    });
#endif
                }
                else
                {
                    // remove the wheelbase to get wheel position.
                    float fraction = rayResult.Fraction - (mechanics.wheelBase) / (mechanics.suspensionLength + mechanics.wheelBase);

                    float3 wheelDesiredPos = math.lerp(rayStart, rayEnd, fraction);
                    // update translation of wheels along suspension column
                    var worldPosition = math.lerp(worldFromLocal.pos, wheelDesiredPos, mechanics.suspensionDamping / mechanics.suspensionStrength);

#if !ENABLE_TRANSFORM_V1
                    newLocalTransform.ValueRW.Position = math.mul(parentFromWorld, new float4(worldPosition, 1f)).xyz;
#else
                    commandBuffer.SetComponent(entity, new Translation
                    {
                        Value = math.mul(parentFromWorld, new float4(worldPosition, 1f)).xyz
                    });
#endif
                    #region Suspension
                    {
                        // Calculate and apply the impulses
                        var posA = rayEnd;
                        var posB = rayResult.Position;
                        var lvA = currentSpeedUp * weUp;
                        var lvB = world.GetLinearVelocity(rayResult.RigidBodyIndex, posB);

                        var impulse = mechanics.suspensionStrength * (posB - posA) + mechanics.suspensionDamping * (lvB - lvA);
                        impulse = impulse * invWheelCount;
                        float impulseUp = math.dot(impulse, weUp);

                        // Suspension shouldn't necessarily pull the vehicle down!
                        float downForceLimit = -0.25f;
                        if (downForceLimit < impulseUp)
                        {
                            impulse = impulseUp * weUp;

                            UnityEngine.Assertions.Assert.IsTrue(math.all(math.isfinite(impulse)));
                            world.ApplyImpulse(ceIdx, impulse, posA);

                            if (mechanics.drawDebugInformation != 0)
                                Debug.DrawRay(wheelDesiredPos, impulse, Color.green);
                        }
                    }
                    #endregion

                    #region Sideways friction
                    {
                        float deltaSpeedRight = (0.0f - currentSpeedRight);
                        deltaSpeedRight = math.clamp(deltaSpeedRight, -mechanics.wheelMaxImpulseRight, mechanics.wheelMaxImpulseRight);
                        deltaSpeedRight *= mechanics.wheelFrictionRight;
                        deltaSpeedRight *= slopeSlipFactor;

                        float3 impulse = deltaSpeedRight * weRight;
                        float effectiveMass = world.GetEffectiveMass(ceIdx, impulse, wheelPos);
                        impulse = impulse * effectiveMass * invWheelCount;

                        UnityEngine.Assertions.Assert.IsTrue(math.all(math.isfinite(impulse)));
                        world.ApplyImpulse(ceIdx, impulse, wheelPos);
                        world.ApplyImpulse(rayResult.RigidBodyIndex, -impulse, wheelPos);

                        if (mechanics.drawDebugInformation != 0)
                            Debug.DrawRay(wheelDesiredPos, impulse, Color.red);
                    }
                    #endregion

                    #region Drive
                    {
                        if (driveEngaged && wheel.ValueRO.UsedForDriving != 0)
                        {
                            float deltaSpeedForward = (driveDesiredSpeed - currentSpeedForward);
                            deltaSpeedForward = math.clamp(deltaSpeedForward, -mechanics.wheelMaxImpulseForward, mechanics.wheelMaxImpulseForward);
                            deltaSpeedForward *= mechanics.wheelFrictionForward;
                            deltaSpeedForward *= slopeSlipFactor;

                            float3 impulse = deltaSpeedForward * weForward;

                            float effectiveMass = world.GetEffectiveMass(ceIdx, impulse, wheelPos);
                            impulse = impulse * effectiveMass * invWheelCount;

                            UnityEngine.Assertions.Assert.IsTrue(math.all(math.isfinite(impulse)));
                            world.ApplyImpulse(ceIdx, impulse, wheelPos);
                            world.ApplyImpulse(rayResult.RigidBodyIndex, -impulse, wheelPos);

                            if (mechanics.drawDebugInformation != 0)
                                Debug.DrawRay(wheelDesiredPos, impulse, Color.blue);
                        }
                    }
                    #endregion
                }

#if !ENABLE_TRANSFORM_V1
                if (!newLocalTransform.ValueRO.Equals(localTransform.ValueRO))
                {
                    commandBuffer.SetComponent(entity, newLocalTransform.ValueRO);
                }
#else
#endif
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}
