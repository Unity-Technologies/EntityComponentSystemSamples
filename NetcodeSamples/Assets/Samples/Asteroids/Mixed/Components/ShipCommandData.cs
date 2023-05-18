using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct ShipCommandData : ICommandData
{
    public NetworkTick Tick {get; set;}
    public byte left;
    public byte right;
    public byte thrust;
    public byte shoot;
}
