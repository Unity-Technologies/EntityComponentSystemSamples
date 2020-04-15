using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class FreeHingeJoint : BaseJoint
    {
        [Tooltip("If checked, PositionInConnectedEntity and HingeAxisInConnectedEntity will be set to match PositionLocal and HingeAxisLocal")]
        public bool AutoSetConnected = true;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;
        public float3 HingeAxisLocal;
        public float3 HingeAxisInConnectedEntity;

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionInConnectedEntity = math.transform(bFromA, PositionLocal);
                HingeAxisInConnectedEntity = math.mul(bFromA.rot, HingeAxisLocal);
            }

            CreateJointEntity(
                JointData.CreateHinge(
                    new JointFrame { Axis = HingeAxisLocal, Position = PositionLocal },
                    new JointFrame { Axis = HingeAxisInConnectedEntity, Position = PositionInConnectedEntity }
                ),
                entityManager, conversionSystem
            );
        }
    }
}
