using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Physics.Authoring
{
    public class RigidJoint : BallAndSocketJoint
    {
        public quaternion OrientationLocal = quaternion.identity;
        public quaternion OrientationInConnectedEntity = quaternion.identity;

        public override void UpdateAuto()
        {
            base.UpdateAuto();
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                OrientationInConnectedEntity = math.mul(bFromA.rot, OrientationLocal);
            }
            {
                OrientationLocal = math.normalize(OrientationLocal);
                OrientationInConnectedEntity = math.normalize(OrientationInConnectedEntity);
            }
        }

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            UpdateAuto();
            conversionSystem.World.GetOrCreateSystem<EndJointConversionSystem>().CreateJointEntity(
                this,
                GetConstrainedBodyPair(conversionSystem),
                PhysicsJoint.CreateFixed(
                    new RigidTransform(OrientationLocal, PositionLocal),
                    new RigidTransform(OrientationInConnectedEntity, PositionInConnectedEntity)
                )
            );
        }
    }
}
