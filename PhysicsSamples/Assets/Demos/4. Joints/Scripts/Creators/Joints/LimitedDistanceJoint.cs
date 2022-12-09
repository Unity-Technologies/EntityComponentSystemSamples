using Unity.Entities;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class LimitedDistanceJoint : BallAndSocketJoint
    {
        public float MinDistance;
        public float MaxDistance;

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            UpdateAuto();
            PhysicsJoint joint = PhysicsJoint.CreateLimitedDistance(
                PositionLocal,
                PositionInConnectedEntity,
                new FloatRange(MinDistance, MaxDistance));

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
    }

    class LimitedDistanceJointBaker : JointBaker<LimitedDistanceJoint>
    {
        public override void Bake(LimitedDistanceJoint authoring)
        {
            authoring.UpdateAuto();

            var physicsJoint = PhysicsJoint.CreateLimitedDistance(authoring.PositionLocal, authoring.PositionInConnectedEntity, new FloatRange(authoring.MinDistance, authoring.MaxDistance));
            var constraintBodyPair = GetConstrainedBodyPair(authoring);

            uint worldIndex = GetWorldIndexFromBaseJoint(authoring);
            CreateJointEntity(worldIndex, constraintBodyPair, physicsJoint);
        }
    }
}
