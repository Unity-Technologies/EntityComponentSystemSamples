using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

// IConvertGameObjectToEntity pipeline is called *before* the Physics Body & Shape Conversion Systems
// This means that there would be no PhysicsMass component to tweak when Convert is called.
// Instead Convert is called from the PhysicsSamplesConversionSystem instead.
public class SetInertiaInverseBehaviour : MonoBehaviour/*, IConvertGameObjectToEntity*/
{
    public bool LockX = false;
    public bool LockY = false;
    public bool LockZ = false;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (dstManager.HasComponent<PhysicsMass>(entity))
        {
            var mass = dstManager.GetComponentData<PhysicsMass>(entity);
            mass.InverseInertia[0] = LockX ? 0 : mass.InverseInertia[0];
            mass.InverseInertia[1] = LockY ? 0 : mass.InverseInertia[1];
            mass.InverseInertia[2] = LockZ ? 0 : mass.InverseInertia[2];
            dstManager.SetComponentData<PhysicsMass>(entity, mass);
        }
    }
}

