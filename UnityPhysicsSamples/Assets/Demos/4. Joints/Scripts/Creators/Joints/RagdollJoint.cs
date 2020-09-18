using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class RagdollJoint : BallAndSocketJoint
    {
        const int k_LatestVersion = 1;

        // Editor only settings
        [HideInInspector]
        public bool EditAxes;
        [HideInInspector]
        public bool EditLimits;

        [SerializeField]
        int m_Version;

        public float3 TwistAxisLocal;
        public float3 TwistAxisInConnectedEntity;
        public float3 PerpendicularAxisLocal;
        public float3 PerpendicularAxisInConnectedEntity;
        public float MaxConeAngle;
        public float MinPerpendicularAngle;
        public float MaxPerpendicularAngle;
        public float MinTwistAngle;
        public float MaxTwistAngle;

        void UpgradeVersionIfNecessary()
        {
            if (m_Version >= k_LatestVersion)
                return;

            MinPerpendicularAngle -= 90f;
            MaxPerpendicularAngle -= 90f;
            m_Version = k_LatestVersion;
        }

        void OnValidate()
        {
            UpgradeVersionIfNecessary();

            MaxConeAngle = math.clamp(MaxConeAngle, 0f, 180f);

            MaxPerpendicularAngle = math.clamp(MaxPerpendicularAngle, -90f, 90f);
            MinPerpendicularAngle = math.clamp(MinPerpendicularAngle, -90f, 90f);
            if (MaxPerpendicularAngle < MinPerpendicularAngle)
            {
                var swap = new FloatRange(MinPerpendicularAngle, MaxPerpendicularAngle).Sorted();
                MinPerpendicularAngle = swap.Min;
                MaxPerpendicularAngle = swap.Max;
            }

            MinTwistAngle = math.clamp(MinTwistAngle, -180f, 180f);
            MaxTwistAngle = math.clamp(MaxTwistAngle, -180f, 180f);
            if (MaxTwistAngle < MinTwistAngle)
            {
                var swap = new FloatRange(MinTwistAngle, MaxTwistAngle).Sorted();
                MinTwistAngle = swap.Min;
                MaxTwistAngle = swap.Max;
            }
        }

        public override void UpdateAuto()
        {
            base.UpdateAuto();
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                TwistAxisInConnectedEntity = math.mul(bFromA.rot, TwistAxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }
        }

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            UpdateAuto();
            UpgradeVersionIfNecessary();
            PhysicsJoint.CreateRagdoll(
                new BodyFrame { Axis = TwistAxisLocal, PerpendicularAxis = PerpendicularAxisLocal, Position = PositionLocal },
                new BodyFrame { Axis = TwistAxisInConnectedEntity, PerpendicularAxis = PerpendicularAxisInConnectedEntity, Position = PositionInConnectedEntity },
                math.radians(MaxConeAngle),
                math.radians(new FloatRange(MinPerpendicularAngle, MaxPerpendicularAngle)),
                math.radians(new FloatRange(MinTwistAngle, MaxTwistAngle)),
                out var primaryCone,
                out var perpendicularCone
            );

            conversionSystem.World.GetOrCreateSystem<EndJointConversionSystem>().CreateJointEntities(
                this,
                GetConstrainedBodyPair(conversionSystem),
                new NativeArray<PhysicsJoint>(2, Allocator.Temp) { [0] = primaryCone, [1] = perpendicularCone }
            );
        }
    }
}
