using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Aspects;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public class SimulationValidationAuthoring : MonoBehaviour
    {
        [Header("General Settings")]

        [Tooltip("Enables simulation validation.")]
        public bool EnableValidation = false;
        [Tooltip("Time period during which any validation is performed as simulation time interval [start, end] in seconds. Specify -1 as end value for a validation that never ends (default).")]
        public float2 ValidationTimeRange = new(0, -1);

        [Header("Validation Types")]

        [Tooltip("Validates if joints behave as expected, by comparing relative body positions and orientations and their relative angular and linear velocities.")]
        public bool ValidateJointBehavior = false;
        [Tooltip("Validates that all rigid bodies are at rest and don't exceed the provided linear and angular velocity error tolerances.")]
        public bool ValidateRigidBodiesAtRest = false;

        [Header("Tolerances")]

        [Tooltip("Linear velocity error tolerance in meters/s")]
        public float LinearVelocityErrorTolerance = 0.005f;
        [Tooltip("Angular velocity error tolerance in radians/s")]
        public float AngularVelocityErrorTolerance = 0.01f;
        [Tooltip("Position error tolerance in meters")]
        public float PositionErrorTolerance = 0.01f;
        [Tooltip("Orientation error tolerance in radians")]
        public float OrientationErrorTolerance = 0.01f;
    }

    public class SimulationValidationBaker : Baker<SimulationValidationAuthoring>
    {
        public override void Bake(SimulationValidationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SimulationValidationSettings()
            {
                EnableValidation = authoring.EnableValidation,
                ValidateJointBehavior = authoring.ValidateJointBehavior,
                ValidateRigidBodiesAtRest = authoring.ValidateRigidBodiesAtRest,
                LinearVelocityErrorTolerance = authoring.LinearVelocityErrorTolerance,
                AngularVelocityErrorTolerance = authoring.AngularVelocityErrorTolerance,
                PositionErrorTolerance = authoring.PositionErrorTolerance,
                OrientationErrorTolerance = authoring.OrientationErrorTolerance,
                ValidationTimeRange = authoring.ValidationTimeRange
            });
        }
    }
    public struct SimulationValidationSettings : IComponentData
    {
        public bool EnableValidation;
        public bool ValidateJointBehavior;
        public bool ValidateRigidBodiesAtRest;
        public float LinearVelocityErrorTolerance;
        public float AngularVelocityErrorTolerance;
        public float PositionErrorTolerance;
        public float OrientationErrorTolerance;
        public float2 ValidationTimeRange;
    }

    /// <summary>
    /// Validation of all PhysicsJoint objects in the simulation.
    ///
    /// The expected behavior corresponds to joints created with the
    /// joint creation functions in PhysicsJoint, e.g., CreatePrismatic, CreateHinge, etc.
    /// </summary>
    [BurstCompile]
    public partial struct ValidateJointBehaviorJob : IJobEntity
    {
        [NativeDisableUnsafePtrRestriction]
        public SimulationValidationSystem.ErrorCounter Errors;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        [ReadOnly] public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;

        [ReadOnly] public DynamicsWorld DynamicsWorld;
        [ReadOnly] public NativeArray<Joint> Joints;

        [ReadOnly] public float PositionErrorTol;
        [ReadOnly] public float PositionErrorTolSq;
        [ReadOnly] public float OrientationErrorTol;
        [ReadOnly] public float OrientationErrorTolCos;
        [ReadOnly] public float AngVelErrorTol;
        [ReadOnly] public float AngVelErrorTolSq;
        [ReadOnly] public float LinVelErrorTol;
        [ReadOnly] public float LinVelErrorTolSq;

        [GenerateTestsForBurstCompatibility]
        static void ValidateConstraintType(in Constraint constraint, in ConstraintType expectedType)
        {
            Assert.AreEqual(expectedType, constraint.Type, $"Validation ({expectedType}): unexpected constraint type '{constraint.Type}'.");
        }

        [GenerateTestsForBurstCompatibility]
        void Execute(in Entity entity, in PhysicsJoint joint, in PhysicsConstrainedBodyPair bodyPair)
        {
            var jointIndex = DynamicsWorld.GetJointIndex(entity);
            var dynamicsJoint = Joints[jointIndex];
            var bodyAIx = dynamicsJoint.BodyPair.BodyIndexA;
            var bodyBIx = dynamicsJoint.BodyPair.BodyIndexB;

            var bodyAIsStatic = bodyAIx < 0 || bodyAIx >= DynamicsWorld.NumMotions;
            var bodyBIsStatic = bodyBIx < 0 || bodyBIx >= DynamicsWorld.NumMotions;
            if (bodyAIsStatic && bodyBIsStatic)
            {
                return;
            }

            var bodyAWorld = bodyPair.EntityA != Entity.Null
                ? TransformLookup[bodyPair.EntityA].ToMatrix()
                : float4x4.identity;
            var bodyBWorld = bodyPair.EntityB != Entity.Null
                ? TransformLookup[bodyPair.EntityB].ToMatrix()
                : float4x4.identity;

            var anchorALocal = joint.BodyAFromJoint;
            var anchorBLocal = joint.BodyBFromJoint;
            var rigidAWorld = new RigidTransform(bodyAWorld);
            var rigidBWorld = new RigidTransform(bodyBWorld);
            var anchorAWorld = math.mul(rigidAWorld, anchorALocal.AsRigidTransform());
            var anchorBWorld = math.mul(rigidBWorld, anchorBLocal.AsRigidTransform());

            // pose validation for PhysicsJoints
            switch (joint.JointType)
            {
                case JointType.BallAndSocket:
                {
                    var deltaPos = anchorAWorld.pos - anchorBWorld.pos;
                    var posErrorSq = math.lengthsq(deltaPos);
                    if (posErrorSq > PositionErrorTolSq)
                    {
                        Errors.Add($"Validation (BallAndSocket): joint anchor position is violated by {math.sqrt(posErrorSq)} meters, which exceeds position error tolerance of {PositionErrorTol} meters.");
                    }

                    break;
                }
                case JointType.Hinge:
                case JointType.LimitedHinge:
                case JointType.AngularVelocityMotor:
                case JointType.RotationalMotor:
                {
                    // obtain hinge axis (attached to body A)
                    byte hingeConstraintBlockIndex = (byte)(joint.JointType == JointType.Hinge ? 0 : 1);
                    var hingeConstraint = joint[hingeConstraintBlockIndex];
                    ValidateConstraintType(hingeConstraint, ConstraintType.Angular);
                    var hingeAxisIndex = hingeConstraint.FreeAxis2D;
                    var hingeAxis = new float3x3(anchorAWorld.rot)[hingeAxisIndex];

                    // make sure rotation happens about the hinge axis
                    var rotBToA = math.mul(math.inverse(anchorAWorld.rot), anchorBWorld.rot);
                    rotBToA = math.normalize(rotBToA);
                    ((Quaternion)rotBToA).ToAngleAxis(out var angle, out var actualRotationAxis);
                    // We can only get a meaningful rotation axis between the two anchors if there is some reasonable amount of delta rotation.
                    // Note: angle is in degrees here
                    var absAngle = math.abs(angle);
                    var epsValidationAngle = 10.0f;
                    if (absAngle > epsValidationAngle && absAngle < 360f - epsValidationAngle)
                    {
                        actualRotationAxis = math.mul(anchorAWorld.rot, actualRotationAxis);
                        actualRotationAxis = math.normalize(actualRotationAxis);

                        // make sure hinge axis is aligned in both anchor frames
                        var cosAngle = math.dot(actualRotationAxis, hingeAxis);
                        var absCosAngle = math.abs(cosAngle);
                        var epsCos = OrientationErrorTolCos;
                        if (absCosAngle < epsCos)
                        {
                            Errors.Add($"Validation (Hinge or equivalent): hinge axis orientation violated by {math.acos(absCosAngle)} radians, which exceeds orientation error tolerance of {OrientationErrorTol} radians");
                        }
                    }

                    // Make sure anchor positions are sufficiently close, as the bodies rotate around them.
                    var deltaPos = anchorAWorld.pos - anchorBWorld.pos;
                    var posErrorSq = math.lengthsq(deltaPos);
                    if (posErrorSq > PositionErrorTolSq)
                    {
                        Errors.Add($"Validation (Hinge or equivalent): joint anchor position is violated by {math.sqrt(posErrorSq)} meters, which exceeds position error tolerance of {PositionErrorTol} meters.");
                    }

                    break;
                }
                case JointType.Fixed:
                {
                    // make sure anchor frames are aligned

                    // orientation
                    var relQ = math.mul(math.inverse(anchorAWorld.rot), anchorBWorld.rot);
                    relQ = math.normalize(relQ);
                    var angle = 2.0 * math.acos(relQ.value.w);
                    var cosAngle = math.cos(angle);
                    if (cosAngle < OrientationErrorTolCos)
                    {
                        Errors.Add($"Validation (Fixed): relative orientation violated by {angle} radians, which exceeds orientation error tolerance of {OrientationErrorTol} radians");
                    }

                    // position
                    var deltaPos = anchorAWorld.pos - anchorBWorld.pos;
                    var posErrorSq = math.lengthsq(deltaPos);
                    if (posErrorSq > PositionErrorTolSq)
                    {
                        Errors.Add($"Validation (Fixed): joint anchor position is violated by {math.sqrt(posErrorSq)} meters, which exceeds position error tolerance of {PositionErrorTol} meters.");
                    }

                    break;
                }
                case JointType.Prismatic:
                case JointType.PositionalMotor:
                {
                    var constrainedAxisIndex = -1;
                    if (joint.JointType == JointType.Prismatic)
                    {
                        var linearConstraint = joint[1];
                        ValidateConstraintType(linearConstraint, ConstraintType.Linear);
                        constrainedAxisIndex = linearConstraint.ConstrainedAxis1D;
                    }
                    else if (joint.JointType == JointType.PositionalMotor)
                    {
                        var motorConstraint = joint[0];
                        ValidateConstraintType(motorConstraint, ConstraintType.PositionMotor);
                        constrainedAxisIndex = motorConstraint.ConstrainedAxis1D;
                    }

                    Assert.IsTrue(constrainedAxisIndex > -1);

                    // We expect the prismatic axis in both anchor frames to be parallel and in the same direction.
                    var axisA = new float3x3(anchorAWorld.rot)[constrainedAxisIndex];
                    var axisB = new float3x3(anchorBWorld.rot)[constrainedAxisIndex];
                    var absCosAngle = math.dot(axisA, axisB);
                    if (absCosAngle < OrientationErrorTolCos)
                    {
                        Errors.Add($"Validation (Prismatic or equivalent): prismatic axis orientation violated by {math.acos(absCosAngle)} radians, which exceeds orientation error tolerance of {OrientationErrorTol} radians");
                    }

                    // Make sure anchors lie on the prismatic axis:
                    // The anchor position in A lies on the prismatic axis (i.e., axisA) by design since both are attached to the same rigid body A.
                    // So we only need to check that the distance of the anchor position in B to the prismatic axis in A lies below the
                    // provided position error tolerance.
                    var ab = anchorBWorld.pos - anchorAWorld.pos;
                    // calculate rejection of ab with respect to plane formed by axisA and anchorAWorld.pos
                    ab -= math.dot(ab, axisA) * axisA;
                    var distToPrismaticAxisSq = math.lengthsq(ab);
                    if (distToPrismaticAxisSq > PositionErrorTolSq)
                    {
                        Errors.Add($"Validation (Prismatic or equivalent): joint anchor lies {math.sqrt(distToPrismaticAxisSq)} meters from prismatic axis, which exceeds position error tolerance of {PositionErrorTol} meters.");
                    }

                    break;
                }
                case JointType.LinearVelocityMotor:
                {
                    var motorConstraint = joint[0];
                    ValidateConstraintType(motorConstraint, ConstraintType.LinearVelocityMotor);
                    var constrainedAxisIndex = motorConstraint.ConstrainedAxis1D;

                    // We expect the linear velocity motor axis (prismatic axis) in both anchor frames to be parallel and in the same direction
                    var axisA = new float3x3(anchorAWorld.rot)[constrainedAxisIndex];
                    var axisB = new float3x3(anchorBWorld.rot)[constrainedAxisIndex];
                    var absCosAngle = math.dot(axisA, axisB);
                    if (absCosAngle < OrientationErrorTolCos)
                    {
                        Errors.Add($"Validation (LinearVelocityMotor): prismatic axis orientation violated by {math.acos(absCosAngle)} radians, which exceeds orientation error tolerance of {OrientationErrorTol} radians");
                    }

                    // We also expect the anchor position in A to lie on the prismatic axis attached to B.
                    var ba = anchorAWorld.pos - anchorBWorld.pos;
                    // calculate rejection of ba with respect to plane formed by axisB and anchorBWorld.pos
                    ba -= math.dot(ba, axisB) * axisB;
                    var distToPrismaticAxisSq = math.lengthsq(ba);
                    if (distToPrismaticAxisSq > PositionErrorTolSq)
                    {
                        Errors.Add($"Validation (LinearVelocityMotor): joint anchor lies {math.sqrt(distToPrismaticAxisSq)} meters from prismatic axis, which exceeds position error tolerance of {PositionErrorTol} meters.");
                    }

                    break;
                }
                case JointType.LimitedDistance:
                {
                    var distanceConstraint = joint[0];
                    ValidateConstraintType(distanceConstraint, ConstraintType.Linear);

                    var min = distanceConstraint.Min;
                    var max = distanceConstraint.Max;

                    var deltaPos = anchorAWorld.pos - anchorBWorld.pos;
                    var distance = math.length(deltaPos);

                    if (distance < min - PositionErrorTol || distance > max + PositionErrorTol)
                    {
                        Errors.Add($"Validation (LimitedDistance): joint distance {distance} is out of admissible (min, max) range ({min}, {max}) by more than position error tolerance of {PositionErrorTol} meters.");
                    }

                    break;
                }
                default:
                    break;
            }

            // target validation for PhysicsJoints
            switch (joint.JointType)
            {
                case JointType.AngularVelocityMotor:
                {
                    // get expected angular velocity
                    var motorConstraint = joint[0];
                    ValidateConstraintType(motorConstraint, ConstraintType.AngularVelocityMotor);
                    int constrainedAxisIndex = motorConstraint.ConstrainedAxis1D;

                    // expected angular velocity in world space
                    var speed = motorConstraint.Target[constrainedAxisIndex];
                    var expectedAngVelRel = new float3x3(anchorAWorld.rot)[constrainedAxisIndex] * speed;

                    // get actual angular velocity
                    var wA = bodyAIsStatic ? float3.zero
                        : PhysicsVelocityLookup[bodyPair.EntityA].GetAngularVelocityWorldSpace(PhysicsMassLookup[bodyPair.EntityA], new quaternion(bodyAWorld));
                    var wB = bodyBIsStatic ? float3.zero
                        : PhysicsVelocityLookup[bodyPair.EntityB].GetAngularVelocityWorldSpace(PhysicsMassLookup[bodyPair.EntityB], new quaternion(bodyBWorld));

                    // actual angular velocity in world space (relative to B)
                    var angVelRel = wA - wB;

                    if (math.abs(math.lengthsq(expectedAngVelRel - angVelRel)) > AngVelErrorTolSq)
                    {
                        Errors.Add($"Validation (AngularVelocityMotor): angular joint velocity {angVelRel} exceeds expected angular velocity {expectedAngVelRel} by more than provided error tolerance of {AngVelErrorTol} rad/s.");
                    }

                    break;
                }
                case JointType.LinearVelocityMotor:
                {
                    var motorConstraint = joint[0];
                    ValidateConstraintType(motorConstraint, ConstraintType.LinearVelocityMotor);
                    int constrainedAxisIndex = motorConstraint.ConstrainedAxis1D;

                    // expected angular velocity in world space
                    var speed = motorConstraint.Target[constrainedAxisIndex];
                    var expectedLinVelRel = new float3x3(anchorBWorld.rot)[constrainedAxisIndex] * speed;

                    // get actual linear velocity
                    var vA = bodyAIsStatic ? float3.zero : PhysicsVelocityLookup[bodyPair.EntityA].Linear;
                    var vB = bodyBIsStatic ? float3.zero : PhysicsVelocityLookup[bodyPair.EntityB].Linear;

                    // actual linear velocity in world space (relative to B)
                    var linVelRel = vA - vB;

                    if (math.abs(math.lengthsq(expectedLinVelRel - linVelRel)) > LinVelErrorTolSq)
                    {
                        Errors.Add($"Validation (LinearVelocityMotor): linear joint velocity {linVelRel} exceeds expected linear velocity {expectedLinVelRel} by more than provided error tolerance of {LinVelErrorTol} m/s.");
                    }

                    break;
                }
                case JointType.RotationalMotor:
                {
                    var motorConstraint = joint[0];
                    ValidateConstraintType(motorConstraint, ConstraintType.RotationMotor);
                    int constrainedAxisIndex = motorConstraint.ConstrainedAxis1D;
                    var targetAngle = motorConstraint.Target[constrainedAxisIndex];

                    // Calculate angle between the joint attachment frames.
                    // Note: we already confirmed that the joint axis is aligned in both anchor frames in the pose validation above.
                    var qDelta = math.normalize(math.mul(math.inverse(anchorBWorld.rot), anchorAWorld.rot));
                    ((Quaternion)qDelta).ToAngleAxis(out var currentAngle, out var axis);
                    // account for flip of axis in ToAngleAxis calculation
                    currentAngle *= axis[constrainedAxisIndex];
                    currentAngle = math.radians(currentAngle);
                    var deltaAngle = currentAngle - targetAngle;
                    var deltaAngleCos = math.cos(deltaAngle);
                    // Note: below we exclude compliant joints, since these won't be able to reach their targets with reasonable accuracy in the general case.
                    var compliantJoint = motorConstraint.SpringFrequency < 1e3;
                    if (deltaAngleCos < OrientationErrorTolCos && !compliantJoint)
                    {
                        Errors.Add($"Validation (RotationalMotor): angle between anchor frames differs from target angle {targetAngle} radians by {deltaAngle} radians, which exceeds the orientation error tolerance of {OrientationErrorTol} radians.");
                    }

                    // check if we are within the limits
                    if (currentAngle + OrientationErrorTol <= motorConstraint.Min || currentAngle - OrientationErrorTol >= motorConstraint.Max)
                    {
                        Errors.Add($"Validation (RotationalMotor): angle between anchor frames {currentAngle} is out of admissible (min, max) range ({motorConstraint.Min}, {motorConstraint.Max}) by more than orientation error tolerance of {OrientationErrorTol} radians.");
                    }
                    break;
                }
                case JointType.PositionalMotor:
                {
                    var motorConstraint = joint[0];
                    ValidateConstraintType(motorConstraint, ConstraintType.PositionMotor);
                    int constrainedAxisIndex = motorConstraint.ConstrainedAxis1D;
                    var targetCoordinate = motorConstraint.Target[constrainedAxisIndex];
                    var prismaticAxis = new float3x3(anchorAWorld.rot)[constrainedAxisIndex];
                    var targetAnchorPosA = prismaticAxis * targetCoordinate + anchorBWorld.pos;
                    var error = math.lengthsq(targetAnchorPosA - anchorAWorld.pos);
                    if (error > PositionErrorTolSq)
                    {
                        Errors.Add($"Validation (PositionalMotor): joint anchor lies {math.sqrt(error)} meters from target position, which exceeds position error tolerance of {PositionErrorTol} meters.");
                    }
                    break;
                }
                default:
                    break;
            }
        }
    }

    [BurstCompile]
    public partial struct ValidateRigidBodyAtRestJob : IJobEntity
    {
        [NativeDisableUnsafePtrRestriction]
        public SimulationValidationSystem.ErrorCounter Errors;

        [ReadOnly] public float MaxLinVel;
        [ReadOnly] public float MaxAngVel;
        [ReadOnly] public float MaxLinVelSq;
        [ReadOnly] public float MaxAngVelSq;

        [GenerateTestsForBurstCompatibility]
        void Execute(RigidBodyAspect rigidBody)
        {
            var vSq = math.lengthsq(rigidBody.LinearVelocity);
            var wSq = math.lengthsq(rigidBody.AngularVelocityLocalSpace);
            bool linVelAtRest = vSq <= MaxLinVelSq;
            bool angVelAtRest = wSq <= MaxAngVelSq;
            if (!linVelAtRest || !angVelAtRest)
            {
                Errors.Add($"Validation (Rigid Body, Entity: {rigidBody.Entity.ToFixedString()}): (linear, angular) velocity is ({math.sqrt(vSq)}, {math.sqrt(wSq)}), which exceeds the (linear, angular) velocity error tolerance of ({MaxLinVel}, {MaxAngVel}).");
            }
        }
    }

    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct SimulationValidationSystem : ISystem
    {
        private ComponentLookup<LocalTransform> TransformLookup;

        private ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
        private ComponentLookup<PhysicsMass> PhysicsMassLookup;
        private int NumErrorsDetected;
        private ErrorCounter Errors;

        private float ElapsedTime;

        public struct ErrorCounter
        {
            private UnsafeAtomicCounter32 Counter;

            public unsafe ErrorCounter(int* errorCount)
            {
                Counter = new UnsafeAtomicCounter32(errorCount);
            }

            public void Add(in FixedString512Bytes errorMessage)
            {
                Debug.LogWarning(errorMessage);
                Counter.Add(1);
            }

            public unsafe int GetCount()
            {
                return *Counter.Counter;
            }

            public void Reset()
            {
                Counter.Reset();
            }
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationValidationSettings>();
            state.RequireForUpdate<PhysicsWorldSingleton>();

            TransformLookup = state.GetComponentLookup<LocalTransform>();

            PhysicsVelocityLookup = state.GetComponentLookup<PhysicsVelocity>();
            PhysicsMassLookup = state.GetComponentLookup<PhysicsMass>();
            unsafe
            {
                fixed(int* numErrorsDetectedPtr = &NumErrorsDetected)
                {
                    Errors = new ErrorCounter(numErrorsDetectedPtr);
                }
            }
            ElapsedTime = 0.0f;
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            // since we require SimulationValidationSettings to exist for this system to be updated,
            // we can be sure that we can retrieve it.
            var settings = SystemAPI.GetSingleton<SimulationValidationSettings>();

            if (!settings.EnableValidation)
            {
                return;
            }
            // else:

            // check if any error has been detected in the validation jobs scheduled last frame (see below)
            var numErrorsDetectedLastFrame = Errors.GetCount();
            // reset the error counter for the upcoming validation jobs
            Errors.Reset();

            // Note: we need to calculate our own elapsed time since the first update of this system has occurred. This is because during SubScene streaming with closed SubScenes
            // the systems in the SubScenes are not immediately created and stepped. They might get stepped only after a few frames delay. Therefore, some time might already have
            // passed (i.e., SystemAPI.Time.ElapsedTime > 0) the first time this system is updated.
            var elapsedTime = ElapsedTime;
            ElapsedTime += SystemAPI.Time.DeltaTime;
            if (settings.ValidationTimeRange[0] <= elapsedTime && (elapsedTime <= settings.ValidationTimeRange[1] || settings.ValidationTimeRange[1] < 0))
            {
                TransformLookup.Update(ref state);

                PhysicsVelocityLookup.Update(ref state);
                PhysicsMassLookup.Update(ref state);

                var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
                var combinedHandle = new JobHandle();

                if (settings.ValidateJointBehavior)
                {
                    var handle = new ValidateJointBehaviorJob()
                    {
                        Errors = Errors,
                        TransformLookup = TransformLookup,

                        PhysicsVelocityLookup = PhysicsVelocityLookup,
                        PhysicsMassLookup = PhysicsMassLookup,
                        DynamicsWorld = physicsWorld.DynamicsWorld,
                        Joints = physicsWorld.DynamicsWorld.Joints,

                        PositionErrorTol = settings.PositionErrorTolerance,
                        PositionErrorTolSq = settings.PositionErrorTolerance * settings.PositionErrorTolerance,
                        OrientationErrorTol = settings.OrientationErrorTolerance,
                        OrientationErrorTolCos = math.cos(settings.OrientationErrorTolerance),
                        AngVelErrorTol = settings.AngularVelocityErrorTolerance,
                        AngVelErrorTolSq = settings.AngularVelocityErrorTolerance * settings.AngularVelocityErrorTolerance,
                        LinVelErrorTol = settings.LinearVelocityErrorTolerance,
                        LinVelErrorTolSq = settings.LinearVelocityErrorTolerance * settings.LinearVelocityErrorTolerance
                    }.ScheduleParallel(state.Dependency);
                    combinedHandle = JobHandle.CombineDependencies(combinedHandle, handle);
                }

                if (settings.ValidateRigidBodiesAtRest)
                {
                    var handle = new ValidateRigidBodyAtRestJob()
                    {
                        Errors = Errors,
                        MaxLinVel = settings.LinearVelocityErrorTolerance,
                        MaxAngVel = settings.AngularVelocityErrorTolerance,
                        MaxLinVelSq = settings.LinearVelocityErrorTolerance * settings.LinearVelocityErrorTolerance,
                        MaxAngVelSq = settings.AngularVelocityErrorTolerance * settings.AngularVelocityErrorTolerance
                    }.ScheduleParallel(state.Dependency);
                    combinedHandle = JobHandle.CombineDependencies(combinedHandle, handle);
                }

                state.Dependency = combinedHandle;
            }

            // Assert if last frame any errors have been detected
            Assert.AreEqual(0, numErrorsDetectedLastFrame, $"SimulationValidationSystem: {numErrorsDetectedLastFrame} errors detected in simulation.");
        }
    }
}
