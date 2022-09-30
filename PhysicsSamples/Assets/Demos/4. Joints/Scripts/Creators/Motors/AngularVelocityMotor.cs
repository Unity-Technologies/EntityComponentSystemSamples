using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class AngularVelocityMotor : BaseJoint
    {
        [Tooltip("An offset from center of entity with motor. Representing the anchor/pivot point of rotation")]
        public float3 PivotPosition;
        [Tooltip("The axis of rotation of the motor. Value will be normalized")]
        public float3 AxisOfRotation;
        [Tooltip("Target speed for the motor to maintain, in degrees/s")]
        public float TargetSpeed;

        private float3 PerpendicularAxisLocal;
        private float3 PositionInConnectedEntity;
        private float3 HingeAxisInConnectedEntity;
        private float3 PerpendicularAxisInConnectedEntity;

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            float3 axis = math.normalize(AxisOfRotation);

            float3 targetInRadians = math.radians(TargetSpeed);
            float3 targetVelocity = targetInRadians * axis;

            RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
            PositionInConnectedEntity = math.transform(bFromA, PivotPosition); //position of motored body pivot relative to Connected Entity in world space
            HingeAxisInConnectedEntity = math.mul(bFromA.rot, axis); //motor axis in Connected Entity space

            // Always calculate the perpendicular axes
            Math.CalculatePerpendicularNormalized(axis, out var perpendicularLocal, out _);
            PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, perpendicularLocal); //perp motor axis in Connected Entity space

            var joint = PhysicsJoint.CreateAngularVelocityMotor(
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
                targetVelocity     //float3 as velocity in rad/s
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
                joint
            );
        }

        class AngularVelocityMotorBaker : JointBaker<AngularVelocityMotor>
        {
            public override void Bake(AngularVelocityMotor authoring)
            {
                float3 axis = math.normalize(authoring.AxisOfRotation);

                float3 targetInRadians = math.radians(authoring.TargetSpeed);
                float3 targetVelocity = targetInRadians * axis;

                RigidTransform bFromA = math.mul(math.inverse(authoring.worldFromB), authoring.worldFromA);
                authoring.PositionInConnectedEntity = math.transform(bFromA, authoring.PivotPosition); //position of motored body pivot relative to Connected Entity in world space
                authoring.HingeAxisInConnectedEntity = math.mul(bFromA.rot, axis); //motor axis in Connected Entity space

                // Always calculate the perpendicular axes
                Math.CalculatePerpendicularNormalized(axis, out var perpendicularLocal, out _);
                authoring.PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, perpendicularLocal); //perp motor axis in Connected Entity space

                var joint = PhysicsJoint.CreateAngularVelocityMotor(
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
                    targetVelocity     //float3 as velocity in rad/s
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
