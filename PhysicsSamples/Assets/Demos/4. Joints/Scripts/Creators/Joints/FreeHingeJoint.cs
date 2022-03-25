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

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            UpdateAuto();
            Math.CalculatePerpendicularNormalized(HingeAxisLocal, out var perpendicularLocal, out _);
            Math.CalculatePerpendicularNormalized(HingeAxisInConnectedEntity, out var perpendicularConnected, out _);
            conversionSystem.World.GetOrCreateSystem<EndJointConversionSystem>().CreateJointEntity(
                this,
                GetConstrainedBodyPair(conversionSystem),
                PhysicsJoint.CreateHinge(
                    new BodyFrame { Axis = HingeAxisLocal, Position = PositionLocal, PerpendicularAxis = perpendicularLocal },
                    new BodyFrame { Axis = HingeAxisInConnectedEntity, Position = PositionInConnectedEntity, PerpendicularAxis = perpendicularConnected }
                )
            );
        }
    }
}
