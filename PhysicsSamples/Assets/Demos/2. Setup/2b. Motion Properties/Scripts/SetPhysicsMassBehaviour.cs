using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

// IConvertGameObjectToEntity pipeline is called *before* the Physics Body & Shape Conversion Systems
// This means that there would be no PhysicsMass component to tweak when Convert is called.
// Instead Convert is called from the PhysicsSamplesConversionSystem instead.
public class SetPhysicsMassBehaviour : MonoBehaviour/*, IConvertGameObjectToEntity*/
{
    [Header("Physics Mass")]
    public bool InfiniteInertiaX = false;
    public bool InfiniteInertiaY = false;
    public bool InfiniteInertiaZ = false;
    public bool InfiniteMass = false;
    [Header("Physics Mass Override")]
    public bool IsKinematic = false;
    public bool SetVelocityToZero = false;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (dstManager.HasComponent<PhysicsMass>(entity))
        {
            if (IsKinematic || SetVelocityToZero)
            {
                if (!dstManager.HasComponent<PhysicsMassOverride>(entity))
                {
                    dstManager.AddComponentData(entity, new PhysicsMassOverride());
                }
                var massOverride = dstManager.GetComponentData<PhysicsMassOverride>(entity);
                massOverride.IsKinematic = (byte)(IsKinematic ? 1 : 0);
                massOverride.SetVelocityToZero = (byte)(SetVelocityToZero ? 1 : 0);
                dstManager.SetComponentData(entity, massOverride);
            }

            var mass = dstManager.GetComponentData<PhysicsMass>(entity);
            mass.InverseInertia[0] = InfiniteInertiaX ? 0 : mass.InverseInertia[0];
            mass.InverseInertia[1] = InfiniteInertiaY ? 0 : mass.InverseInertia[1];
            mass.InverseInertia[2] = InfiniteInertiaZ ? 0 : mass.InverseInertia[2];
            mass.InverseMass = InfiniteMass ? 0 : mass.InverseMass;
            dstManager.SetComponentData<PhysicsMass>(entity, mass);
        }
    }
}
