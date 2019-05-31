using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;

namespace Unity.Physics.Authoring
{
    public class RagdollJoint : BaseJoint
    {
        [Tooltip("If checked, PositionInConnectedEntity and TwistAxisInConnectedEntity will be set to match PositionLocal and TwistAxisLocal")]
        public bool AutoSetConnected = true;

        // Editor only settings
        [HideInInspector]
        public bool EditPivots;
        [HideInInspector]
        public bool EditAxes;
        [HideInInspector]
        public bool EditLimits;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;
        public float3 TwistAxisLocal;
        public float3 TwistAxisInConnectedEntity;
        public float3 PerpendicularAxisLocal;
        public float3 PerpendicularAxisInConnectedEntity;
        public float MaxConeAngle;
        public float MinPerpendicularAngle;
        public float MaxPerpendicularAngle;
        public float MinTwistAngle;
        public float MaxTwistAngle;

        public override unsafe void Create(EntityManager entityManager)
        {
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionInConnectedEntity = math.transform(bFromA, PositionLocal);
                TwistAxisInConnectedEntity = math.mul(bFromA.rot, TwistAxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }

            BlobAssetReference<JointData> jointData0, jointData1;
            JointData.CreateRagdoll(
                    PositionLocal, PositionInConnectedEntity, TwistAxisLocal, TwistAxisInConnectedEntity, PerpendicularAxisLocal, PerpendicularAxisInConnectedEntity,
                    math.radians(MaxConeAngle), math.radians(MinPerpendicularAngle), math.radians(MaxPerpendicularAngle), math.radians(MinTwistAngle), math.radians(MaxTwistAngle),
                    out jointData0, out jointData1);

            CreateJointEntity(jointData0, entityManager);
            CreateJointEntity(jointData1, entityManager);
        }
    }
}
