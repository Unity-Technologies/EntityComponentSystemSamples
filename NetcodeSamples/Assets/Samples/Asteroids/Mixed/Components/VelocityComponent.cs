using Unity.Netcode;
using Unity.Entities;
using Unity.Mathematics;


[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
public struct Velocity : IComponentData
{
    [GhostField(Quantization=100, Smoothing=SmoothingAction.InterpolateAndExtrapolate)]
    public float2 Value;
}
