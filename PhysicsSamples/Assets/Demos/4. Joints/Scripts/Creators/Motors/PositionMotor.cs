using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class PositionMotor : BaseJoint
    {
        [Tooltip("An offset from the center of the body with the motor, representing the anchor point of translation.")]
        public float3 AnchorPosition;
        [Tooltip("The direction of the motor, relative to the orientation of the Connected Body (bodyB). Value will be normalized")]
        public float3 DirectionOfMovement;
        [Tooltip("Motor will drive this length away from the anchor position of bodyA.")]
        public float TargetDistance;
        [Tooltip("The magnitude of the maximum impulse the motor can exert in a single step. Applies only to the motor constraint.")]
        public float MaxImpulseAppliedByMotor = math.INFINITY;

        private float3 PerpendicularAxisLocal;
        private float3 PositionInConnectedEntity;
        private float3 AxisInConnectedEntity;
        private float3 PerpendicularAxisInConnectedEntity;

        class PositionMotorBaker : JointBaker<PositionMotor>
        {
            public override void Bake(PositionMotor authoring)
            {
                float3 axisInB = math.normalize(authoring.DirectionOfMovement);

                RigidTransform aFromB = math.mul(math.inverse(authoring.worldFromA), authoring.worldFromB);
                float3 axisInA = math.mul(aFromB.rot, axisInB); //motor axis relative to bodyA

                RigidTransform bFromA = math.mul(math.inverse(authoring.worldFromB), authoring.worldFromA);
                authoring.PositionInConnectedEntity = math.transform(bFromA, authoring.AnchorPosition); //position of motored body relative to Connected Entity in world space
                authoring.AxisInConnectedEntity = axisInB; //motor axis in Connected Entity space

                // Always calculate the perpendicular axes
                Math.CalculatePerpendicularNormalized(axisInA, out var perpendicularLocal, out _);
                authoring.PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, perpendicularLocal); //perp motor axis in Connected Entity space

                var joint = PhysicsJoint.CreatePositionMotor(
                    new BodyFrame
                    {
                        Axis = axisInA,
                        PerpendicularAxis = perpendicularLocal,
                        Position = authoring.AnchorPosition
                    },
                    new BodyFrame
                    {
                        Axis = authoring.AxisInConnectedEntity,
                        PerpendicularAxis = authoring.PerpendicularAxisInConnectedEntity,
                        Position = authoring.PositionInConnectedEntity
                    },
                    authoring.TargetDistance,
                    authoring.MaxImpulseAppliedByMotor
                );

                joint.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);

                var constraintBodyPair = GetConstrainedBodyPair(authoring);

                uint worldIndex = GetWorldIndexFromBaseJoint(authoring);
                CreateJointEntity(worldIndex, constraintBodyPair, joint);
            }
        }
    }
}
