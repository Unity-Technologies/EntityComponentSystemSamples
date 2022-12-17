using Unity.Entities;
using Unity.NetCode;

public struct ShipTagComponentData : IComponentData
{
}

public struct ShipStateComponentData : IComponentData
{
    [GhostField] public int State;
    [GhostField] public NetworkTick WeaponCooldown;
}
