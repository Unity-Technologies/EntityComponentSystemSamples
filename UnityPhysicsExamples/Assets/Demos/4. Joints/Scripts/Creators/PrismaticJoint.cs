using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class PrismaticJoint : BaseJoint
    {
        [Tooltip("If checked, PositionLocal will snap to match PositionInConnectedEntity")]
        public bool AutoSetConnected = true;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;
        public float3 AxisInConnectedEntity;
        public float MinDistanceOnAxis;
        public float MaxDistanceOnAxis;
        public float MinDistanceFromAxis;
        public float MaxDistanceFromAxis;

        public override unsafe void Create(EntityManager entityManager)
        {
            if (AutoSetConnected)
            {
                PositionLocal = math.transform(math.inverse(worldFromA), math.transform(worldFromB, PositionInConnectedEntity));
            }

            CreateJointEntity(JointData.CreatePrismatic(PositionLocal, PositionInConnectedEntity, AxisInConnectedEntity,
                    MinDistanceOnAxis, MaxDistanceOnAxis, MinDistanceFromAxis, MaxDistanceFromAxis), entityManager);
        }
    }
}
