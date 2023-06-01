using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class FreeHingeJoint : BallAndSocketJoint
    {
        // Editor only settings
        [HideInInspector]
        public bool EditAxes;

        public float3 HingeAxisLocal;
        public float3 HingeAxisInConnectedEntity;

        public override void UpdateAuto()
        {
            base.UpdateAuto();
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                HingeAxisInConnectedEntity = math.mul(bFromA.rot, HingeAxisLocal);
            }
        }
    }

    class FreeHingeJointBaker : JointBaker<FreeHingeJoint>
    {
        public override void Bake(FreeHingeJoint authoring)
        {
            authoring.UpdateAuto();

            Math.CalculatePerpendicularNormalized(authoring.HingeAxisLocal, out var perpendicularLocal, out _);
            Math.CalculatePerpendicularNormalized(authoring.HingeAxisInConnectedEntity, out var perpendicularConnected, out _);

            var physicsJoint = PhysicsJoint.CreateHinge(
                new BodyFrame {Axis = authoring.HingeAxisLocal, Position = authoring.PositionLocal, PerpendicularAxis = perpendicularLocal},
                new BodyFrame {Axis = authoring.HingeAxisInConnectedEntity, Position = authoring.PositionInConnectedEntity, PerpendicularAxis = perpendicularConnected }
            );

            physicsJoint.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);

            var constraintBodyPair = GetConstrainedBodyPair(authoring);

            uint worldIndex = GetWorldIndexFromBaseJoint(authoring);
            CreateJointEntity(worldIndex, constraintBodyPair, physicsJoint);
        }
    }
}
