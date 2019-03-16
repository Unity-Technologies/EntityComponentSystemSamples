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
                var pb = math.transform(worldFromB, PositionInConnectedEntity);
                PositionLocal = math.transform(math.inverse(worldFromA), pb);
                HingeAxisLocal = math.rotate(math.inverse(worldFromA), math.rotate(worldFromB, HingeAxisInConnectedEntity));
                PerpendicularAxisLocal = math.rotate(math.inverse(worldFromA), math.rotate(worldFromB, PerpendicularAxisInConnectedEntity));
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
