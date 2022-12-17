using Unity.Entities;
using Unity.NetCode;

public struct PlayerStateComponentData : IComponentData
{
    public int IsSpawning;
}

public struct PlayerIdComponentData : IComponentData
{
    public Entity PlayerEntity;
}
