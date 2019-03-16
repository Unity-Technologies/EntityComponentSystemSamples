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
                PositionLocal = math.transform(math.inverse(worldFromA), math.transform(worldFromB, PositionInConnectedEntity));
            }

            CreateJointEntity(JointData.CreateBallAndSocket(PositionLocal, PositionInConnectedEntity), entityManager);
        }
    }
}
