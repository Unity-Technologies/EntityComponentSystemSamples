using Unity.Entities;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class LimitedDistanceJoint : BallAndSocketJoint
    {
        public float MinDistance;
        public float MaxDistance;
    }

    class LimitedDistanceJointBaker : JointBaker<LimitedDistanceJoint>
    {
        public override void Bake(LimitedDistanceJoint authoring)
        {
            authoring.UpdateAuto();

            var physicsJoint = PhysicsJoint.CreateLimitedDistance(authoring.PositionLocal, authoring.PositionInConnectedEntity, new FloatRange(authoring.MinDistance, authoring.MaxDistance));
            physicsJoint.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);

            var constraintBodyPair = GetConstrainedBodyPair(authoring);

            uint worldIndex = GetWorldIndexFromBaseJoint(authoring);
            CreateJointEntity(worldIndex, constraintBodyPair, physicsJoint);
        }
    }
}
