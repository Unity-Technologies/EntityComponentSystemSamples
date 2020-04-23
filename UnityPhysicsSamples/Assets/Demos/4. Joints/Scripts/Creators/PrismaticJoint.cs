using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class PrismaticJoint : BaseJoint
    {
        [Tooltip("If checked, PositionLocal will snap to match PositionInConnectedEntity")]
        public bool AutoSetConnected = true;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;
        public float3 AxisLocal;
        public float3 AxisInConnectedEntity;
        public float3 PerpendicularAxisLocal;
        public float3 PerpendicularAxisInConnectedEntity;
        public float MinDistanceOnAxis;
        public float MaxDistanceOnAxis;
        public float MinDistanceFromAxis;
        public float MaxDistanceFromAxis;

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionInConnectedEntity = math.transform(bFromA, PositionLocal);
                AxisInConnectedEntity = math.mul(bFromA.rot, AxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }

            CreateJointEntity(JointData.CreatePrismatic(
                    new JointFrame
                    {
                        Axis = AxisLocal,
                        PerpendicularAxis = PerpendicularAxisLocal,
                        Position = PositionLocal
                    },
                    new JointFrame
                    {
                        Axis = AxisInConnectedEntity,
                        PerpendicularAxis = PerpendicularAxisInConnectedEntity,
                        Position = PositionInConnectedEntity
                    },
                    new FloatRange(MinDistanceOnAxis, MaxDistanceOnAxis),
                    new FloatRange(MinDistanceFromAxis, MaxDistanceFromAxis)
                ),
                entityManager, conversionSystem
            );
        }
    }
}
