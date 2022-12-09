using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class LinearVelocityMotor : BaseJoint
    {
        [Tooltip("An offset from the center of the entity with the motor, representing the anchor point of translation.")]
        public float3 AnchorPosition;
        [Tooltip("The direction of the motor. Value will be normalized")]
        public float3 DirectionOfMovement;
        [Tooltip("The speed for the motor to maintain, in m/s")]
        public float TargetSpeed;

        private float3 PerpendicularAxisLocal;
        private float3 PositionInConnectedEntity;
        private float3 AxisInConnectedEntity;
        private float3 PerpendicularAxisInConnectedEntity;

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            float3 axis = math.normalize(DirectionOfMovement);
            float3 targetVelocity = TargetSpeed * axis;

            RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
            PositionInConnectedEntity = math.transform(bFromA, AnchorPosition); //position of motored body relative to Connected Entity in world space
            AxisInConnectedEntity = math.mul(bFromA.rot, axis); //motor axis in Connected Entity space

            // Always calculate the perpendicular axes
            Math.CalculatePerpendicularNormalized(axis, out var perpendicularAxisLocal, out _);
            PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, perpendicularAxisLocal); //perp motor axis in Connected Entity space

            var joint = PhysicsJoint.CreateLinearVelocityMotor(
                new BodyFrame
                {
                    Axis = axis,
                    PerpendicularAxis = perpendicularAxisLocal,
                    Position = AnchorPosition
                },
                new BodyFrame
                {
                    Axis = AxisInConnectedEntity,
                    PerpendicularAxis = PerpendicularAxisInConnectedEntity,
                    Position = PositionInConnectedEntity
                },
                targetVelocity //value encompasses motor axis with motor target
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

        class LinearVelocityMotorBaker : JointBaker<LinearVelocityMotor>
        {
            public override void Bake(LinearVelocityMotor authoring)
            {
                float3 axis = math.normalize(authoring.DirectionOfMovement);
                float3 targetVelocity = authoring.TargetSpeed * axis;

                RigidTransform bFromA = math.mul(math.inverse(authoring.worldFromB), authoring.worldFromA);
                authoring.PositionInConnectedEntity = math.transform(bFromA, authoring.AnchorPosition); //position of motored body relative to Connected Entity in world space
                authoring.AxisInConnectedEntity = math.mul(bFromA.rot, axis); //motor axis in Connected Entity space

                // Always calculate the perpendicular axes
                Math.CalculatePerpendicularNormalized(axis, out var perpendicularAxisLocal, out _);
                authoring.PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, perpendicularAxisLocal); //perp motor axis in Connected Entity space

                var joint = PhysicsJoint.CreateLinearVelocityMotor(
                    new BodyFrame
                    {
                        Axis = axis,
                        PerpendicularAxis = perpendicularAxisLocal,
                        Position = authoring.AnchorPosition
                    },
                    new BodyFrame
                    {
                        Axis = authoring.AxisInConnectedEntity,
                        PerpendicularAxis = authoring.PerpendicularAxisInConnectedEntity,
                        Position = authoring.PositionInConnectedEntity
                    },
                    targetVelocity //value encompasses motor axis with motor target
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
