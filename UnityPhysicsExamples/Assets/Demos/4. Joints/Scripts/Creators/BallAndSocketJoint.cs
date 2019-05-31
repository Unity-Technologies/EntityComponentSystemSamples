using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class BallAndSocketJoint : BaseJoint
    {
        [Tooltip("If checked, PositionLocal will snap to match PositionInConnectedEntity")]
        public bool AutoSetConnected = true;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;

        public override unsafe void Create(EntityManager entityManager)
        {
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionInConnectedEntity = math.transform(bFromA, PositionLocal);
            }

            CreateJointEntity(JointData.CreateBallAndSocket(PositionLocal, PositionInConnectedEntity), entityManager);
        }
    }
}
