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
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionLocal = math.transform(bFromA, PositionInConnectedEntity);
                OrientationLocal = math.mul(bFromA.rot, OrientationInConnectedEntity);
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
