using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    [GhostComponent(PrefabType=GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct PhysicsPlayerInput : ICommandData
    {
        [GhostField] public NetworkTick Tick { get; set; }
        [GhostField] public int Horizontal;
        [GhostField] public int Vertical;
    }

    [DisallowMultipleComponent]
    public class PhysicsPlayerInputAuthoring : MonoBehaviour
    {
        class Baker : Baker<PhysicsPlayerInputAuthoring>
        {
            public override void Bake(PhysicsPlayerInputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<PhysicsPlayerInput>(entity);
            }
        }
    }
}
