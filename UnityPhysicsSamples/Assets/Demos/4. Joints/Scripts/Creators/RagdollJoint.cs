using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Physics.Math;

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

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionInConnectedEntity = math.transform(bFromA, PositionLocal);
                TwistAxisInConnectedEntity = math.mul(bFromA.rot, TwistAxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }

            JointData.CreateRagdoll(
                new JointFrame { Axis = TwistAxisLocal, PerpendicularAxis = PerpendicularAxisLocal, Position = PositionLocal },
                new JointFrame { Axis = TwistAxisInConnectedEntity, PerpendicularAxis = PerpendicularAxisInConnectedEntity, Position = PositionInConnectedEntity },
                math.radians(MaxConeAngle),
                math.radians(new FloatRange(MinPerpendicularAngle, MaxPerpendicularAngle)),
                math.radians(new FloatRange(MinTwistAngle, MaxTwistAngle)),
                out BlobAssetReference<JointData> jointData0,
                out BlobAssetReference<JointData> jointData1
            );

            CreateJointEntity(jointData0, entityManager, conversionSystem);
            CreateJointEntity(jointData1, entityManager, conversionSystem);
        }
    }
}
