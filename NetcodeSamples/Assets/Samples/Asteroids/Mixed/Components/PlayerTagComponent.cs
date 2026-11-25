using Unity.Netcode;
using Unity.Entities;


public struct ShipTagComponentData : IComponentData
{
}

public struct ShipStateComponentData : IComponentData
{
    [GhostField] public int State;
    [GhostField] public NetworkTick WeaponCooldown;
}
