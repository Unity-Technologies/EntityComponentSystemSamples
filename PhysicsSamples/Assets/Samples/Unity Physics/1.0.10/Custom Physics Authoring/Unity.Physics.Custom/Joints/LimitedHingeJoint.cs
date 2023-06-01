using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class LimitedHingeJoint : FreeHingeJoint
    {
        // Editor only settings
        [HideInInspector]
        public bool EditLimits;

        public float3 PerpendicularAxisLocal;
        public float3 PerpendicularAxisInConnectedEntity;
        public float MinAngle;
        public float MaxAngle;

        public override void UpdateAuto()
        {
            base.UpdateAuto();
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                HingeAxisInConnectedEntity = math.mul(bFromA.rot, HingeAxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }
        }
    }

    class LimitedHingeJointBaker : JointBaker<LimitedHingeJoint>
    {
        public override void Bake(LimitedHingeJoint authoring)
        {
            authoring.UpdateAuto();

            var physicsJoint = PhysicsJoint.CreateLimitedHinge(
                new BodyFrame
                {
                    Axis = math.normalize(authoring.HingeAxisLocal),
                    PerpendicularAxis = math.normalize(authoring.PerpendicularAxisLocal),
                    Position = authoring.PositionLocal
                },
                new BodyFrame
                {
                    Axis = math.normalize(authoring.HingeAxisInConnectedEntity),
                    PerpendicularAxis = math.normalize(authoring.PerpendicularAxisInConnectedEntity),
                    Position = authoring.PositionInConnectedEntity
                },
                math.radians(new FloatRange(authoring.MinAngle, authoring.MaxAngle))
            );

            physicsJoint.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);

            var constraintBodyPair = GetConstrainedBodyPair(authoring);

            uint worldIndex = GetWorldIndexFromBaseJoint(authoring);
            CreateJointEntity(worldIndex, constraintBodyPair, physicsJoint);
        }
    }
}
