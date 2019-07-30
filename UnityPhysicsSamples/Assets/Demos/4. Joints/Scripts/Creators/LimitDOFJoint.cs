using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    // This Joint allows you to lock one or more of the 6 degrees of freedom of a constrained body.
    // This is achieved by combining the appropriate lower level 'constraint atoms' to form the higher level Joint.
    // In this case Linear and Angular constraint atoms are combined. 
    // One use-case for this Joint could be to restrict a 3d simulation to a 2d plane.
    public class LimitDOFJoint : BaseJoint
    {
        public bool3 LockLinearAxes = new bool3();
        public bool3 LockAngularAxes = new bool3();

        public static BlobAssetReference<JointData> CreateLimitDOFJoint(
            RigidTransform offset, bool3 linearLocks, bool3 angularLocks)
        {
            var constraintCount = (math.any(linearLocks) ? 1 : 0) + (math.any(angularLocks) ? 1 : 0);
            Constraint[] constraints = new Constraint[constraintCount];
            int index = 0;
            if (math.any(linearLocks))
            {
                constraints[index++] = new Constraint
                {
                    ConstrainedAxes = linearLocks,
                    Type = ConstraintType.Linear,
                    Min = 0,
                    Max = 0,
                    SpringFrequency = Constraint.DefaultSpringFrequency,
                    SpringDamping = Constraint.DefaultSpringDamping
                };
            }
            if (math.any(angularLocks))
            {
                constraints[index++] = new Constraint
                {
                    ConstrainedAxes = angularLocks,
                    Type = ConstraintType.Angular,
                    Min = 0,
                    Max = 0,
                    SpringFrequency = Constraint.DefaultSpringFrequency,
                    SpringDamping = Constraint.DefaultSpringDamping
                };
            }
            return JointData.Create(
                    new Math.MTransform(float3x3.identity, float3.zero),
                    new Math.MTransform(offset),
                    constraints
                );
        }

        public override unsafe void Create(EntityManager entityManager)
        {
            RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);

            if (math.any(LockLinearAxes) || math.any(LockAngularAxes))
            {
                CreateJointEntity(CreateLimitDOFJoint(bFromA, LockLinearAxes, LockAngularAxes), entityManager);
            }
        }
    }
}
