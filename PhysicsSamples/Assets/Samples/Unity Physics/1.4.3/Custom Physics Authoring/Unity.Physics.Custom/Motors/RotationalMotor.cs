using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class RotationalMotor : BaseJoint
    {
        [Tooltip("An offset from center of entity with motor. Representing the anchor/pivot point of rotation")]
        public float3 PivotPosition;
        [Tooltip("The axis of rotation of the motor. Value will be normalized")]
        public float3 AxisOfRotation;
        [Tooltip("Motor will maintain this target angle around the AxisOfRotation, in degrees")]
        public float TargetAngle;
        [Tooltip("The magnitude of the maximum impulse the motor can exert in one step. Applies only to the motor constraint.")]
        public float MaxImpulseAppliedByMotor = math.INFINITY;
        [Tooltip("The spring frequency, in Hz. Default value is 74341.31 which describes a stiff spring.")]
        public float SpringFrequency = Constraint.DefaultSpringFrequency;
        [Tooltip("A ratio describing how quickly a motor will arrive at the target. A value of 0 will oscillate about a solution indefinitely, while a value of 1 critically damped. Default value is 2530.126 which describes a stiff spring")]
        public float DampingRatio = Constraint.DefaultDampingRatio;

        private float3 PerpendicularAxisLocal;
        private float3 PositionInConnectedEntity;
        private float3 HingeAxisInConnectedEntity;
        private float3 PerpendicularAxisInConnectedEntity;

        class RotationalMotorBaker : JointBaker<RotationalMotor>
        {
            public override void Bake(RotationalMotor authoring)
            {
                float3 axisInA = math.normalize(authoring.AxisOfRotation);

                RigidTransform bFromA = math.mul(math.inverse(authoring.worldFromB), authoring.worldFromA);
                authoring.PositionInConnectedEntity = math.transform(bFromA, authoring.PivotPosition); //position of motored body pivot relative to Connected Entity in world space
                authoring.HingeAxisInConnectedEntity = math.mul(bFromA.rot, axisInA); //motor axis in Connected Entity space

                // Always calculate the perpendicular axes
                Math.CalculatePerpendicularNormalized(axisInA, out var perpendicularLocal, out _);
                authoring.PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, perpendicularLocal); //perp motor axis in Connected Entity space

                var joint = PhysicsJoint.CreateRotationalMotor(
                    new BodyFrame
                    {
                        Axis = axisInA,
                        PerpendicularAxis = perpendicularLocal,
                        Position = authoring.PivotPosition
                    },
                    new BodyFrame
                    {
                        Axis = authoring.HingeAxisInConnectedEntity,
                        PerpendicularAxis = authoring.PerpendicularAxisInConnectedEntity,
                        Position = authoring.PositionInConnectedEntity
                    },
                    math.radians(authoring.TargetAngle),
                    authoring.MaxImpulseAppliedByMotor,

                    authoring.SpringFrequency,
                    authoring.DampingRatio
                );

                joint.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);
                var constraintBodyPair = GetConstrainedBodyPair(authoring);

                uint worldIndex = GetWorldIndexFromBaseJoint(authoring);
                CreateJointEntity(worldIndex, constraintBodyPair, joint);
            }
        }
    }
}
