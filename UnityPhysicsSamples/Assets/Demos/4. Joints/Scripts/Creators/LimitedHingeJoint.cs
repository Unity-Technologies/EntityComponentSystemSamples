using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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

        public override unsafe void Create(EntityManager entityManager)
        {
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionInConnectedEntity = math.transform(bFromA, PositionLocal);
                HingeAxisInConnectedEntity = math.mul(bFromA.rot, HingeAxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }

            CreateJointEntity(JointData.CreateLimitedHinge(
                    PositionLocal, PositionInConnectedEntity, 
                    math.normalize(HingeAxisLocal), math.normalize(HingeAxisInConnectedEntity),
                    math.normalize(PerpendicularAxisLocal), math.normalize(PerpendicularAxisInConnectedEntity), 
                    math.radians(MinAngle), math.radians(MaxAngle)),
                    entityManager);
        }
    }
}
