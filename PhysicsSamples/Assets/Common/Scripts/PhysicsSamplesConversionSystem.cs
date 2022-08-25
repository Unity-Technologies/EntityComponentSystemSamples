using Unity.Entities;

namespace Unity.Physics.Authoring
{
    // IConvertGameObjectToEntity pipeline is called *before* the Physics Body & Shape Conversion Systems
    // This means that there would be no Physics components created when Convert was called.
    // Instead Convert is called from this specific ConversionSystem for any component that may need
    // to read or write the various Physics components at conversion time.

    [UpdateAfter(typeof(PhysicsBodyConversionSystem))]
    [UpdateAfter(typeof(LegacyRigidbodyConversionSystem))]
    [UpdateAfter(typeof(EndJointConversionSystem))]
    public class PhysicsSamplesConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((SetPhysicsMassBehaviour behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((ChangeSphereColliderRadiusAuthoring behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((ChangeColliderTypeAuthoring behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((ChangeMotionTypeAuthoring behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((DriveGhostBodyAuthoring behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
        }
    }
}
