using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Physics.Authoring
{
    // This Joint allows you to lock one or more of the 6 degrees of freedom of a constrained body.
    // This is achieved by combining the appropriate lower level 'constraint atoms' to form the higher level Joint.
    // In this case Linear and Angular constraint atoms are combined.
    // One use-case for this Joint could be to restrict a 3d simulation to a 2d plane.
    public class LimitDOFJoint : BaseJoint
    {
        public bool3 LockLinearAxes;
        public bool3 LockAngularAxes;

        public static PhysicsJoint CreateLimitDOFJoint(RigidTransform offset, bool3 linearLocks, bool3 angularLocks)
        {
            var constraints = new FixedList128<Constraint>();
            if (math.any(linearLocks))
            {
                constraints.Add(new Constraint
                {
                    ConstrainedAxes = linearLocks,
                    Type = ConstraintType.Linear,
                    Min = 0,
                    Max = 0,
                    SpringFrequency = Constraint.DefaultSpringFrequency,
                    SpringDamping = Constraint.DefaultSpringDamping
                });
            }
            if (math.any(angularLocks))
            {
                constraints.Add(new Constraint
                {
                    ConstrainedAxes = angularLocks,
                    Type = ConstraintType.Angular,
                    Min = 0,
                    Max = 0,
                    SpringFrequency = Constraint.DefaultSpringFrequency,
                    SpringDamping = Constraint.DefaultSpringDamping
                });
            }

            var joint = new PhysicsJoint
            {
                BodyAFromJoint = BodyFrame.Identity,
                BodyBFromJoint = offset
            };
            joint.SetConstraints(constraints);
            return joint;
        }

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            if (!math.any(LockLinearAxes) && !math.any(LockAngularAxes))
                return;

            RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
            conversionSystem.World.GetOrCreateSystem<EndJointConversionSystem>().CreateJointEntity(
                this,
                GetConstrainedBodyPair(conversionSystem),
                CreateLimitDOFJoint(bFromA, LockLinearAxes, LockAngularAxes)
            );
        }
    }
}
