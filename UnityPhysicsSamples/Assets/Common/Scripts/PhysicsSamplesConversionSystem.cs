﻿using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    // IConvertGameObjectToEntity pipeline is called *before* the Physics Body & Shape Conversion Systems
    // This means that there would be no Physics components created when Convert was called.
    // Instead Convert is called from this specific ConversionSystem for any component that may need
    // to read or write the various Physics components at conversion time.

    [UpdateAfter(typeof(PhysicsBodyConversionSystem))]
    [UpdateAfter(typeof(LegacyRigidbodyConversionSystem))]
    [UpdateAfter(typeof(LegacyJointConversionSystem))]
    public class PhysicsSamplesConversionSystem : GameObjectConversionSystem
    {
        // Update is called once per frame
        protected override void OnUpdate()
        {
            Entities.ForEach((SetInertiaInverseBehaviour behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((ChangeSphereColliderRadiusBehaviour behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((ChangeColliderTypeBehaviour behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((ChangeMotionTypeBehaviour behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
        }
    }
}
