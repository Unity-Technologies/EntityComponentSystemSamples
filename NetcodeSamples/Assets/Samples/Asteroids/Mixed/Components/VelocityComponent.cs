using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
public struct Velocity : IComponentData
{
    [GhostField(Quantization=100, Smoothing=SmoothingAction.InterpolateAndExtrapolate)]
    public float2 Value;
}
