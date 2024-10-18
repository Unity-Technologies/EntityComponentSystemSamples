using Unity.Collections;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct ShipCommandData : ICommandData
{
    public NetworkTick Tick {get; set;}
    public byte left;
    public byte right;
    public byte thrust;
    public byte shoot;

    public override string ToString() => ToFixedString().ToString();

    public FixedString512Bytes ToFixedString() => $"steer:{left - right},thrust:{thrust},shoot:{shoot}";
}
