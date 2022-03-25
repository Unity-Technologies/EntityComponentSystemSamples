using Unity.Entities;
using Unity.Mathematics;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class PrismaticJoint : BallAndSocketJoint
    {
        public float3 AxisLocal;
        public float3 AxisInConnectedEntity;
        public float3 PerpendicularAxisLocal;
        public float3 PerpendicularAxisInConnectedEntity;
        public float MinDistanceOnAxis;
        public float MaxDistanceOnAxis;

        public override void UpdateAuto()
        {
            base.UpdateAuto();
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                AxisInConnectedEntity = math.mul(bFromA.rot, AxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }
        }

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            UpdateAuto();
            conversionSystem.World.GetOrCreateSystem<EndJointConversionSystem>().CreateJointEntity(
                this,
                GetConstrainedBodyPair(conversionSystem),
                PhysicsJoint.CreatePrismatic(
                    new BodyFrame
                    {
                        Axis = AxisLocal,
                        PerpendicularAxis = PerpendicularAxisLocal,
                        Position = PositionLocal
                    },
                    new BodyFrame
                    {
                        Axis = AxisInConnectedEntity,
                        PerpendicularAxis = PerpendicularAxisInConnectedEntity,
                        Position = PositionInConnectedEntity
                    },
                    new FloatRange(MinDistanceOnAxis, MaxDistanceOnAxis)
                )
            );
        }
    }
}
