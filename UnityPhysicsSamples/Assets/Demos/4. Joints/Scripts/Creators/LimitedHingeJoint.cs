using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class LimitedHingeJoint : FreeHingeJoint
    {
        // Editor only settings
        [HideInInspector]
        public bool EditPivots;
        [HideInInspector]
        public bool EditAxes;
        [HideInInspector]
        public bool EditLimits;

        public float3 PerpendicularAxisLocal;
        public float3 PerpendicularAxisInConnectedEntity;
        public float MinAngle;
        public float MaxAngle;

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionInConnectedEntity = math.transform(bFromA, PositionLocal);
                HingeAxisInConnectedEntity = math.mul(bFromA.rot, HingeAxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }

            CreateJointEntity(JointData.CreateLimitedHinge(
                    new JointFrame
                    {
                        Axis = math.normalize(HingeAxisLocal),
                        PerpendicularAxis = math.normalize(PerpendicularAxisLocal),
                        Position = PositionLocal
                    },
                    new JointFrame
                    {
                        Axis = math.normalize(HingeAxisInConnectedEntity),
                        PerpendicularAxis = math.normalize(PerpendicularAxisInConnectedEntity),
                        Position = PositionInConnectedEntity
                    },
                    math.radians(new FloatRange(MinAngle, MaxAngle))
                ),
                entityManager, conversionSystem
            );
        }
    }
}
