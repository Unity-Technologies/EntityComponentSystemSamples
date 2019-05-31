using System;
using System.Collections.Generic;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;
using Unity.Burst;
using Unity.Jobs;

namespace Demos
{
    public class VehicleMechanics : MonoBehaviour, IRecieveEntity
    {
        [Header("Chassis Parameters...")]
        public GameObject chassis;
        public float3 chassisUp = new float3(0, 1, 0);
        public float3 chassisRight = new float3(1, 0, 0);
        public float3 chassisForward = new float3(0, 0, 1);
        [Header("Wheel Parameters...")]
        public List<GameObject> wheels;
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
        public List<GameObject> steeringWheels;
        public float steeringAngle = 0.0f;
        [Header("Drive Parameters...")]
        public List<GameObject> driveWheels;
        public bool driveEngaged = true;
        public float driveDesiredSpeed = 1.0f;
        [Header("Miscellaneous Parameters...")]
        public bool drawDebugInformation = false;

        public Entity chassisEntity = Entity.Null;
        public void SetRecievedEntity(Entity entity)
        {
            chassisEntity = entity;
        }
    }

    #region System
    [UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(StepPhysicsWorld))]
    public class VehicleMechanicsSystem : ComponentSystem
    {
        BuildPhysicsWorld CreatePhysicsWorldSystem;
        private EntityQuery VehicleGroup;

        protected override void OnCreate()
        {
            CreatePhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            VehicleGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(VehicleMechanics)
                }
            });
        }

        [BurstCompile]
        public struct RaycastJob : IJobParallelFor
        {
            [ReadOnly] public CollisionWorld world;
            [ReadOnly] public NativeArray<RaycastInput> inputs;
            public NativeArray<RaycastHit> results;

            public unsafe void Execute(int index)
            {
                RaycastHit hit;
                world.CastRay(inputs[index], out hit);
                results[index] = hit;
            }
        }

        public static JobHandle ScheduleBatchRayCast(CollisionWorld world,
            NativeArray<RaycastInput> inputs, NativeArray<RaycastHit> results)
        {
            JobHandle rcj = new RaycastJob
            {
                inputs = inputs,
                results = results,
                world = world

            }.Schedule(inputs.Length, 5);
            return rcj;
        }


        // Update is called once per frame
        protected override void OnUpdate()
        {
            // Make sure the world has finished building before querying it
            CreatePhysicsWorldSystem.FinalJobHandle.Complete();

            var em = World.Active.EntityManager;
            PhysicsWorld world = CreatePhysicsWorldSystem.PhysicsWorld;

            float invDt = 1.0f / Time.fixedDeltaTime;

            Entities.ForEach((VehicleMechanics mechanics) =>
            {
                if (mechanics.wheels.Count == 0) return;

                Entity ce = mechanics.chassisEntity;
                if (ce == Entity.Null) return;

                int ceIdx = world.GetRigidBodyIndex(ce);
                if (-1 == ceIdx || ceIdx >= world.NumDynamicBodies) return;

                //float ceMass = world.GetMass(ceIdx);
                float3 cePosition = em.GetComponentData<Translation>(ce).Value;
                quaternion ceRotation = em.GetComponentData<Rotation>(ce).Value;
                float3 ceCenterOfMass = world.GetCenterOfMass(ceIdx);
                float3 ceUp = math.mul(ceRotation, mechanics.chassisUp);
                float3 ceForward = math.mul(ceRotation, mechanics.chassisForward);
                float3 ceRight = math.mul(ceRotation, mechanics.chassisRight);

                var rayResults = new NativeArray<RaycastHit>(mechanics.wheels.Count, Allocator.TempJob);
                var rayVelocities = new NativeArray<float3>(mechanics.wheels.Count, Allocator.TempJob);

                // Collect the RayCast results
                var rayInputs = new NativeArray<RaycastInput>(mechanics.wheels.Count, Allocator.TempJob);
                CollisionFilter filter = world.GetCollisionFilter(ceIdx);
                for (int i = 0; i < mechanics.wheels.Count; i++)
                {
                    GameObject weGO = mechanics.wheels[i];

                    float3 wheelCurrentPos = weGO.transform.position;

                    float3 rayStart = weGO.transform.parent.position;
                    float3 rayEnd = (-ceUp * (mechanics.suspensionLength + mechanics.wheelBase)) + rayStart;

                    if (mechanics.drawDebugInformation)
                        Debug.DrawRay(rayStart, rayEnd - rayStart);

                    rayInputs[i] = new RaycastInput
                    {
                        Start = rayStart,
                        End = rayEnd,
                        Filter = filter
                    };
                }
                JobHandle rayJobHandle = ScheduleBatchRayCast(world.CollisionWorld, rayInputs, rayResults);
                rayJobHandle.Complete();
                for (int i = 0; i < mechanics.wheels.Count; i++)
                {
                    RaycastHit rayResult = rayResults[i];

                    rayVelocities[i] = float3.zero;
                    if( rayResult.RigidBodyIndex != -1 )
                    {
                        float3 wheelPos = rayResult.Position;
                        wheelPos -= (cePosition - ceCenterOfMass);

                        float3 velocityAtWheel = world.GetLinearVelocity(ceIdx, wheelPos);
                        rayVelocities[i] = velocityAtWheel;
                    }
                }
                rayInputs.Dispose();


                // Calculate a simple slip factor based on chassis tilt.
                float slopeSlipFactor = math.pow( math.abs( math.dot(ceUp, math.up()) ), 4.0f );

                // Proportional apply velocity changes to each wheel
                float invWheelCount = 1.0f / mechanics.wheels.Count;
                for( int i = 0; i < mechanics.wheels.Count; i++ )
                {
                    GameObject weGO = mechanics.wheels[i];

                    float3 rayStart = weGO.transform.parent.position;
                    float3 rayEnd = (-ceUp * (mechanics.suspensionLength + mechanics.wheelBase)) + rayStart;

                    float3 rayDir = rayEnd - rayStart;

                    RaycastHit rayResult = rayResults[i];
                    //float3 velocityAtWheel = rayVelocities[i];

                    float3 wheelPos = rayResult.Position;
                    wheelPos -= (cePosition - ceCenterOfMass);

                    float3 velocityAtWheel = world.GetLinearVelocity(ceIdx, wheelPos);

                    float3 weUp = ceUp;
                    float3 weRight = ceRight;
                    float3 weForward = ceForward;

                    #region handle wheel steering
                    {
                        bool bIsSteeringWheel = mechanics.steeringWheels.Contains(weGO);
                        if (bIsSteeringWheel)
                        {
                            float steeringAngle = math.radians(mechanics.steeringAngle);
                            //if((mechanics.steeringWheels.IndexOf(weGO)+1) > (0.5f * mechanics.steeringWheels.Count))
                            //    steeringAngle = -steeringAngle;

                            quaternion wRotation = quaternion.AxisAngle(ceUp, steeringAngle);
                            weRight = math.rotate(wRotation, weRight);
                            weForward = math.rotate(wRotation, weForward);

                            weGO.transform.localRotation = quaternion.AxisAngle(mechanics.chassisUp, steeringAngle);
                        }
                    }
                    #endregion

                    float currentSpeedUp = math.dot(velocityAtWheel, weUp);
                    float currentSpeedForward = math.dot(velocityAtWheel, weForward);
                    float currentSpeedRight = math.dot(velocityAtWheel, weRight);

                    #region handle wheel rotation
                    {
                        var rGO = weGO.transform.GetChild(0);
                        if (rGO)
                        {
                            bool isDriven = (mechanics.driveEngaged && mechanics.driveWheels.Contains(weGO));
                            float weRotation = isDriven
                                ? (mechanics.driveDesiredSpeed / mechanics.wheelBase)
                                : (currentSpeedForward / mechanics.wheelBase);

                            weRotation = math.radians(weRotation);
                            rGO.transform.localRotation *= quaternion.AxisAngle(mechanics.chassisRight, weRotation);
                        }
                    }
                    #endregion


                    float3 wheelCurrentPos = weGO.transform.position;
                    bool hit = !math.all(rayResult.SurfaceNormal == float3.zero);
                    if (!hit)
                    {
                        float3 wheelDesiredPos = (-ceUp * mechanics.suspensionLength) + rayStart;
                        weGO.transform.position = math.lerp(wheelCurrentPos, wheelDesiredPos, mechanics.suspensionDamping / mechanics.suspensionStrength);
                    }
                    else
                    {
                        // remove the wheelbase to get wheel position.
                        float fraction = rayResult.Fraction - (mechanics.wheelBase) / (mechanics.suspensionLength + mechanics.wheelBase);

                        float3 wheelDesiredPos = math.lerp(rayStart, rayEnd, fraction);
                        weGO.transform.position = math.lerp(wheelCurrentPos, wheelDesiredPos, mechanics.suspensionDamping / mechanics.suspensionStrength);

                        #region Suspension
                        {
                            // Calculate and apply the impulses
                            var posA = rayEnd;
                            var posB = rayResult.Position;
                            var lvA = currentSpeedUp * weUp;// world.GetLinearVelocity(ceIdx, posA);
                            var lvB = world.GetLinearVelocity(rayResult.RigidBodyIndex, posB);

                            var impulse = mechanics.suspensionStrength * (posB - posA) + mechanics.suspensionDamping * (lvB - lvA);
                            impulse = impulse * invWheelCount;
                            float impulseUp = math.dot(impulse, weUp);

                            // Suspension shouldn't necessarily pull the vehicle down!
                            float downForceLimit = -0.25f;
                            if (downForceLimit < impulseUp)
                            {
                                impulse = impulseUp * weUp;

                                world.ApplyImpulse(ceIdx, impulse, posA);
                                //world.ApplyImpulse(rayResult.RigidBodyIndex, -impulse, posB);

                                if (mechanics.drawDebugInformation)
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

                            world.ApplyImpulse(ceIdx, impulse, wheelPos);
                            world.ApplyImpulse(rayResult.RigidBodyIndex, -impulse, wheelPos);

                            if (mechanics.drawDebugInformation)
                                Debug.DrawRay(wheelDesiredPos, impulse, Color.red);
                        }
                        #endregion

                        #region Drive
                        {
                            if (mechanics.driveEngaged && mechanics.driveWheels.Contains(weGO))
                            {
                                float deltaSpeedForward = (mechanics.driveDesiredSpeed - currentSpeedForward);
                                deltaSpeedForward = math.clamp(deltaSpeedForward, -mechanics.wheelMaxImpulseForward, mechanics.wheelMaxImpulseForward);
                                deltaSpeedForward *= mechanics.wheelFrictionForward;
                                deltaSpeedForward *= slopeSlipFactor;

                                float3 impulse = deltaSpeedForward * weForward;

                                float effectiveMass = world.GetEffectiveMass(ceIdx, impulse, wheelPos);
                                impulse = impulse * effectiveMass * invWheelCount;

                                world.ApplyImpulse(ceIdx, impulse, wheelPos);
                                world.ApplyImpulse(rayResult.RigidBodyIndex, -impulse, wheelPos);

                                if (mechanics.drawDebugInformation)
                                    Debug.DrawRay(wheelDesiredPos, impulse, Color.blue);
                            }
                        }
                        #endregion
                    }
                }

                rayResults.Dispose();
                rayVelocities.Dispose();
            });
        }
    }

    #endregion


}
