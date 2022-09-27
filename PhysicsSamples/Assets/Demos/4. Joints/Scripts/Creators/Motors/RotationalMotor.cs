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

        private float3 PerpendicularAxisLocal;
        private float3 PositionInConnectedEntity;
        private float3 HingeAxisInConnectedEntity;
        private float3 PerpendicularAxisInConnectedEntity;

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            float3 axis = math.normalize(AxisOfRotation);

            float targetInRadians = math.radians(TargetAngle);
            float3 targetRotation = targetInRadians * axis;

            RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
            PositionInConnectedEntity = math.transform(bFromA, PivotPosition); //position of motored body pivot relative to Connected Entity in world space
            HingeAxisInConnectedEntity = math.mul(bFromA.rot, axis); //motor axis in Connected Entity space

            // Always calculate the perpendicular axes
            Math.CalculatePerpendicularNormalized(axis, out var perpendicularLocal, out _);
            PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, perpendicularLocal); //perp motor axis in Connected Entity space

            var joint = PhysicsJoint.CreateRotationalMotor(
                new BodyFrame
                {
                    Axis = axis,
                    PerpendicularAxis = perpendicularLocal,
                    Position = PivotPosition
                },
                new BodyFrame
                {
                    Axis = HingeAxisInConnectedEntity,
                    PerpendicularAxis = PerpendicularAxisInConnectedEntity,
                    Position = PositionInConnectedEntity
                },
                targetRotation     //in radians
            );

            var constraints = joint.GetConstraints();
            for (int i = 0; i < constraints.Length; ++i)
            {
                constraints.ElementAt(i).MaxImpulse = MaxImpulse;
                constraints.ElementAt(i).EnableImpulseEvents = RaiseImpulseEvents;
            }
            joint.SetConstraints(constraints);

            conversionSystem.World.GetOrCreateSystemManaged<EndJointConversionSystem>().CreateJointEntity(
                this,
                GetConstrainedBodyPair(conversionSystem),
                joint);
        }

        class RotationalMotorBaker : JointBaker<RotationalMotor>
        {
            public override void Bake(RotationalMotor authoring)
            {
                float3 axis = math.normalize(authoring.AxisOfRotation);

                float targetInRadians = math.radians(authoring.TargetAngle);
                float3 targetRotation = targetInRadians * axis;

                RigidTransform bFromA = math.mul(math.inverse(authoring.worldFromB), authoring.worldFromA);
                authoring.PositionInConnectedEntity = math.transform(bFromA, authoring.PivotPosition); //position of motored body pivot relative to Connected Entity in world space
                authoring.HingeAxisInConnectedEntity = math.mul(bFromA.rot, axis); //motor axis in Connected Entity space

                // Always calculate the perpendicular axes
                Math.CalculatePerpendicularNormalized(axis, out var perpendicularLocal, out _);
                authoring.PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, perpendicularLocal); //perp motor axis in Connected Entity space

                var joint = PhysicsJoint.CreateRotationalMotor(
                    new BodyFrame
                    {
                        Axis = axis,
                        PerpendicularAxis = perpendicularLocal,
                        Position = authoring.PivotPosition
                    },
                    new BodyFrame
                    {
                        Axis = authoring.HingeAxisInConnectedEntity,
                        PerpendicularAxis = authoring.PerpendicularAxisInConnectedEntity,
                        Position = authoring.PositionInConnectedEntity
                    },
                    targetRotation     //in radians
                );

                var constraints = joint.GetConstraints();
                for (int i = 0; i < constraints.Length; ++i)
                {
                    constraints.ElementAt(i).MaxImpulse = authoring.MaxImpulse;
                    constraints.ElementAt(i).EnableImpulseEvents = authoring.RaiseImpulseEvents;
                }
                joint.SetConstraints(constraints);

                var constraintBodyPair = GetConstrainedBodyPair(authoring);

                uint worldIndex = GetWorldIndexFromBaseJoint(authoring);
                CreateJointEntity(worldIndex, constraintBodyPair, joint);
            }
        }
    }
}
