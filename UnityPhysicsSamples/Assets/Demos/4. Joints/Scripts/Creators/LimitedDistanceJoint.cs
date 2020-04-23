using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class LimitedDistanceJoint : BaseJoint
    {
        [Tooltip("If checked, PositionLocal will snap to match PositionInConnectedEntity")]
        public bool AutoSetConnected = true;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;
        public float MinDistance;
        public float MaxDistance;

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            if (AutoSetConnected)
            {
                PositionLocal = math.transform(math.inverse(worldFromA), math.transform(worldFromB, PositionInConnectedEntity));
            }

            CreateJointEntity(
                JointData.CreateLimitedDistance(
                    PositionLocal,
                    PositionInConnectedEntity,
                    new FloatRange(MinDistance, MaxDistance)
                ),
                entityManager, conversionSystem
            );
        }
    }
}
