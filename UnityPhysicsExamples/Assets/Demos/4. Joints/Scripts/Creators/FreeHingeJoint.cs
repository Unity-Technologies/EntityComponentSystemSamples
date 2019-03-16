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

        public override unsafe void Create(EntityManager entityManager)
        {
            if (AutoSetConnected)
            {
                var pb = math.transform(worldFromB, PositionInConnectedEntity);
                PositionLocal = math.transform(math.inverse(worldFromA), pb);
                HingeAxisLocal = math.rotate(math.inverse(worldFromA), math.rotate(worldFromB, HingeAxisInConnectedEntity));
            }

            CreateJointEntity(JointData.CreateHinge(
                PositionLocal, PositionInConnectedEntity, 
                HingeAxisLocal, HingeAxisInConnectedEntity),
                entityManager);
        }
    }
}
