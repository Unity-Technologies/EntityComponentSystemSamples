using System.Collections.Generic;
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

    // ensure built-in physics conversion systems have run
    [UpdateAfter(typeof(EndColliderConversionSystem))]
    [UpdateAfter(typeof(PhysicsBodyConversionSystem))]
    class VehicleMechanicsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((VehicleMechanics m) =>
            {
                var entity = GetPrimaryEntity(m);

                foreach (var wheel in m.wheels)
                {
                    var wheelEntity = GetPrimaryEntity(wheel);
                    DstEntityManager.AddComponentData(wheelEntity, new Wheel
                    {
                        Vehicle = entity,
                        GraphicalRepresentation = GetPrimaryEntity(wheel.transform.GetChild(0)), // assume wheel has a single child with rotating graphic
                        // TODO assume for now that driving/steering wheels also appear in this list
                        UsedForSteering = (byte)(m.steeringWheels.Contains(wheel) ? 1 : 0),
                        UsedForDriving = (byte)(m.driveWheels.Contains(wheel) ? 1 : 0)
                    });
                }

                DstEntityManager.AddComponent<VehicleBody>(entity);
                DstEntityManager.AddComponentData(entity, new VehicleConfiguration
                {
                    wheelBase = m.wheelBase,
                    wheelFrictionRight = m.wheelFrictionRight,
                    wheelFrictionForward = m.wheelFrictionForward,
                    wheelMaxImpulseRight = m.wheelMaxImpulseRight,
                    wheelMaxImpulseForward = m.wheelMaxImpulseForward,
                    suspensionLength = m.suspensionLength,
                    suspensionStrength = m.suspensionStrength,
                    suspensionDamping = m.suspensionDamping,
                    invWheelCount = 1f / m.wheels.Count,
                    drawDebugInformation = (byte)(m.drawDebugInformation ? 1 : 0)
                });
            });
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
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(StepPhysicsWorld))]
    public class VehicleMechanicsSystem : SystemBase
    {
        BuildPhysicsWorld m_BuildPhysicsWorldSystem;

        struct TransformsInitialized : ISystemStateComponentData {}

        protected override void OnCreate()
        {
            m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VehicleConfiguration) }
            }));
        }

        protected override void OnUpdate()
        {
            // ensure transform systems have run at least once so there are no NaN values
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            Dependency = Entities
                .WithName("InitializeWheelsJob")
                .WithBurst()
                .WithNone<TransformsInitialized>()
                .ForEach((Entity entity, in Wheel wheel, in LocalToParent localToParent) =>
                {
                    if (!localToParent.Value.Equals(default))
                        commandBuffer.AddComponent<TransformsInitialized>(entity);
                }).Schedule(Dependency);
            Dependency.Complete();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            Dependency = m_BuildPhysicsWorldSystem.GetOutputDependency();

            // update vehicle properties first
            Dependency = Entities
                .WithName("PrepareVehiclesJob")
                .WithBurst()
                .ForEach((
                    Entity entity, ref VehicleBody vehicleBody,
                    in VehicleConfiguration mechanics, in PhysicsMass mass, in Translation translation, in Rotation rotation
                    ) =>
                    {
                        vehicleBody.WorldCenterOfMass = mass.GetCenterOfMassWorldSpace(in translation, in rotation);

                        // calculate a simple slip factor based on chassis tilt
                        float3 worldUp = math.mul(rotation.Value, math.up());
                        vehicleBody.SlopeSlipFactor = math.pow(math.abs(math.dot(worldUp, math.up())), 4f);
                    })
                .Schedule(Dependency);

            Dependency.Complete();

            // this sample makes direct modifications to impulses between BuildPhysicsWorld and StepPhysicsWorld
            // we thus use PhysicsWorldExtensions rather than modifying component data, since they have already been consumed by BuildPhysicsWorld
            PhysicsWorld world = m_BuildPhysicsWorldSystem.PhysicsWorld;
            var collisionWorld = world.CollisionWorld;

            // update each wheel
            commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            // Dependency =
            Entities
                .WithName("VehicleWheelsJob")
                .WithBurst()
                .WithReadOnly(collisionWorld)
                .WithAll<TransformsInitialized>()
                .ForEach((
                    Entity entity, in Wheel wheel, in LocalToWorld worldFromLocal, in LocalToParent parentFromLocal
                    ) =>
                    {
                        Entity ce = wheel.Vehicle;
                        if (ce == Entity.Null) return;

                        int ceIdx = world.GetRigidBodyIndex(ce);
                        if (-1 == ceIdx || ceIdx >= world.NumDynamicBodies) return;

                        var mechanics = GetComponent<VehicleConfiguration>(ce);
                        var vehicleBody = GetComponent<VehicleBody>(ce);

                        float3 cePosition = GetComponent<Translation>(ce).Value;
                        quaternion ceRotation = GetComponent<Rotation>(ce).Value;
                        float3 ceCenterOfMass = vehicleBody.WorldCenterOfMass;
                        float3 ceUp = math.mul(ceRotation, new float3(0f, 1f, 0f));
                        float3 ceForward = math.mul(ceRotation, new float3(0f, 0f, 1f));
                        float3 ceRight = math.mul(ceRotation, new float3(1f, 0f, 0f));

                        CollisionFilter filter = world.GetCollisionFilter(ceIdx);

                        float driveDesiredSpeed = 0f;
                        bool driveEngaged = false;
                        if (HasComponent<VehicleSpeed>(ce))
                        {
                            var vehicleSpeed = GetComponent<VehicleSpeed>(ce);
                            driveDesiredSpeed = vehicleSpeed.DesiredSpeed;
                            driveEngaged = vehicleSpeed.DriveEngaged != 0;
                        }

                        float desiredSteeringAngle = HasComponent<VehicleSteering>(ce)
                            ? GetComponent<VehicleSteering>(ce).DesiredSteeringAngle
                            : 0f;

                        float3 wheelCurrentPos = worldFromLocal.Position;

                        // create a raycast from the suspension point on the chassis
                        var worldFromParent = math.mul(worldFromLocal.Value, math.inverse(parentFromLocal.Value));
                        float3 rayStart = worldFromParent.c3.xyz;
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

                        // graphical updates assume a hierarchy of the form:
                        // - vehicle
                        //  - suspension
                        //   - wheel (rotates about yaw axis and translates along suspension up)
                        //    - graphic (rotates about pitch axis)

                        #region handle wheel steering
                        {
                            // update yaw angle if wheel is used for steering
                            if (wheel.UsedForSteering != 0)
                            {
                                quaternion wRotation = quaternion.AxisAngle(ceUp, desiredSteeringAngle);
                                weRight = math.rotate(wRotation, weRight);
                                weForward = math.rotate(wRotation, weForward);

                                commandBuffer.SetComponent(entity, new Rotation { Value = quaternion.AxisAngle(math.up(), desiredSteeringAngle) });
                            }
                        }
                        #endregion

                        float currentSpeedUp = math.dot(velocityAtWheel, weUp);
                        float currentSpeedForward = math.dot(velocityAtWheel, weForward);
                        float currentSpeedRight = math.dot(velocityAtWheel, weRight);

                        #region handle wheel rotation
                        {
                            // update rotation of graphical representation about axle
                            bool isDriven = driveEngaged && wheel.UsedForDriving != 0;
                            float weRotation = isDriven
                                ? (driveDesiredSpeed / mechanics.wheelBase)
                                : (currentSpeedForward / mechanics.wheelBase);

                            weRotation = math.radians(weRotation);
                            var currentRotation = GetComponent<Rotation>(wheel.GraphicalRepresentation).Value;
                            commandBuffer.SetComponent(wheel.GraphicalRepresentation, new Rotation
                            {
                                // assumes wheels are aligned with chassis in "bind pose"
                                Value = math.mul(currentRotation, quaternion.AxisAngle(new float3(1f, 0f, 0f), weRotation))
                            });
                        }
                        #endregion

                        var parentFromWorld = math.mul(parentFromLocal.Value, math.inverse(worldFromLocal.Value));
                        if (!hit)
                        {
                            float3 wheelDesiredPos = (-ceUp * mechanics.suspensionLength) + rayStart;
                            var worldPosition = math.lerp(wheelCurrentPos, wheelDesiredPos, mechanics.suspensionDamping / mechanics.suspensionStrength);
                            // update translation of wheels along suspension column
                            commandBuffer.SetComponent(entity, new Translation
                            {
                                Value = math.mul(parentFromWorld, new float4(worldPosition, 1f)).xyz
                            });
                        }
                        else
                        {
                            // remove the wheelbase to get wheel position.
                            float fraction = rayResult.Fraction - (mechanics.wheelBase) / (mechanics.suspensionLength + mechanics.wheelBase);

                            float3 wheelDesiredPos = math.lerp(rayStart, rayEnd, fraction);
                            // update translation of wheels along suspension column
                            var worldPosition = math.lerp(wheelCurrentPos, wheelDesiredPos, mechanics.suspensionDamping / mechanics.suspensionStrength);
                            commandBuffer.SetComponent(entity, new Translation
                            {
                                Value = math.mul(parentFromWorld, new float4(worldPosition, 1f)).xyz
                            });

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
                                if (driveEngaged && wheel.UsedForDriving != 0)
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
                    })
                .Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}
