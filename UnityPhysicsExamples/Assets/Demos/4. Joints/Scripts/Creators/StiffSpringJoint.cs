using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class StiffSpringJoint : BaseJoint
    {
        [Tooltip("If checked, PositionLocal will snap to match PositionInConnectedEntity")]
        public bool AutoSetConnected = true;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;
        public float MinDistance;
        public float MaxDistance;

        public override unsafe void Create(EntityManager entityManager)
        {
            if (AutoSetConnected)
            {
                PositionLocal = math.transform(math.inverse(worldFromA), math.transform(worldFromB, PositionInConnectedEntity));
            }

            CreateJointEntity(JointData.CreateStiffSpring(
                PositionLocal, PositionInConnectedEntity, 
                MinDistance, MaxDistance),
                entityManager);
        }
    }
}
