using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    [UpdateAfter(typeof(PhysicsBodyConversionSystem))]
    [UpdateAfter(typeof(LegacyRigidbodyConversionSystem))]
    public class PhysicsJointConversionSystem : GameObjectConversionSystem
    {
        private void CreateJoint( BaseJoint joint )
        {
            if (!joint.enabled)
                return;

            joint.entityA = GetPrimaryEntity(joint.gameObject);
            joint.entityB = joint.ConnectedBody == null ? Entity.Null : GetPrimaryEntity(joint.ConnectedBody);

            joint.Create(DstEntityManager);
        }

        // Update is called once per frame
        protected override void OnUpdate()
        {
            Entities.ForEach((BallAndSocketJoint joint) => { CreateJoint(joint); });
            Entities.ForEach((FreeHingeJoint joint) => { CreateJoint(joint); });
            Entities.ForEach((LimitedHingeJoint joint) => { CreateJoint(joint); });
            Entities.ForEach((StiffSpringJoint joint) => { CreateJoint(joint); });
            Entities.ForEach((PrismaticJoint joint) => { CreateJoint(joint); });
            Entities.ForEach((RagdollJoint joint) => { CreateJoint(joint); }); // Note: RagdollJoint.Create add 2 entities
            Entities.ForEach((RigidJoint joint) => { CreateJoint(joint); });
            Entities.ForEach((LimitDOFJoint joint) => { CreateJoint(joint); });
        }
    }
}