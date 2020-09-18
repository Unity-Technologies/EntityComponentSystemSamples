using Unity.Entities;

namespace Unity.Physics.Authoring
{
    [UpdateAfter(typeof(PhysicsBodyConversionSystem))]
    [UpdateAfter(typeof(LegacyRigidbodyConversionSystem))]
    [UpdateAfter(typeof(BeginJointConversionSystem))]
    [UpdateBefore(typeof(EndJointConversionSystem))]
    public class PhysicsJointConversionSystem : GameObjectConversionSystem
    {
        void CreateJoint(BaseJoint joint)
        {
            if (!joint.enabled)
                return;

            joint.EntityA = GetPrimaryEntity(joint.LocalBody);
            joint.EntityB = joint.ConnectedBody == null ? Entity.Null : GetPrimaryEntity(joint.ConnectedBody);

            joint.Create(DstEntityManager, this);
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((BallAndSocketJoint joint) => { foreach (var j in joint.GetComponents<BallAndSocketJoint>()) CreateJoint(j); });
            Entities.ForEach((FreeHingeJoint joint) => { foreach (var j in joint.GetComponents<FreeHingeJoint>()) CreateJoint(j); });
            Entities.ForEach((LimitedHingeJoint joint) => { foreach (var j in joint.GetComponents<LimitedHingeJoint>()) CreateJoint(j); });
            Entities.ForEach((LimitedDistanceJoint joint) => { foreach (var j in joint.GetComponents<LimitedDistanceJoint>()) CreateJoint(j); });
            Entities.ForEach((PrismaticJoint joint) => { foreach (var j in joint.GetComponents<PrismaticJoint>()) CreateJoint(j); });
            Entities.ForEach((RagdollJoint joint) => { foreach (var j in joint.GetComponents<RagdollJoint>()) CreateJoint(j); }); // Note: RagdollJoint.Create add 2 entities
            Entities.ForEach((RigidJoint joint) => { foreach (var j in joint.GetComponents<RigidJoint>()) CreateJoint(j); });
            Entities.ForEach((LimitDOFJoint joint) => { foreach (var j in joint.GetComponents<LimitDOFJoint>()) CreateJoint(j); });
        }
    }
}
