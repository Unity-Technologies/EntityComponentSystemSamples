using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class RigidJoint : BaseJoint
    {
        [Tooltip("If checked, PositionLocal will snap to match PositionInConnectedEntity")]
        public bool AutoSetConnected = true;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;
        public quaternion OrientationLocal;
        public quaternion OrientationInConnectedEntity;

        public override unsafe void Create(EntityManager entityManager)
        {
            if (AutoSetConnected)
            {
                RigidTransform aFromB = math.mul(math.inverse(worldFromA), worldFromB);
                PositionLocal = math.transform(aFromB, PositionInConnectedEntity);
                OrientationLocal = math.mul(aFromB.rot, OrientationInConnectedEntity);
            }
            else
            {
                OrientationLocal = math.normalize(OrientationLocal);
                OrientationInConnectedEntity = math.normalize(OrientationInConnectedEntity);
            }

            CreateJointEntity(JointData.CreateFixed(
                PositionLocal, PositionInConnectedEntity, 
                OrientationLocal, OrientationInConnectedEntity),
                entityManager);
        }
    }
}
