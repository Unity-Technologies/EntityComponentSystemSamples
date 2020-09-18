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
            conversionSystem.World.GetOrCreateSystem<EndJointConversionSystem>().CreateJointEntity(
                this,
                GetConstrainedBodyPair(conversionSystem),
                PhysicsJoint.CreateLimitedDistance(
                    PositionLocal,
                    PositionInConnectedEntity,
                    new FloatRange(MinDistance, MaxDistance)
                )
            );
        }
    }
}
