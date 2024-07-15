using Unity.Entities;
using Unity.NetCode;

namespace Samples.CustomChunkSerializer
{
    [GhostEnabledBit]
    [GhostComponent(SendDataForChildEntity = true)]
    public struct IntCompo1 : IComponentData, IEnableableComponent
    {
        [GhostField] public int Value;
    }

    [GhostEnabledBit]
    [GhostComponent(SendDataForChildEntity = true)]
    public struct IntCompo2 : IComponentData, IEnableableComponent
    {
        [GhostField] public int Value;
    }

    [GhostEnabledBit]
    [GhostComponent(SendDataForChildEntity = true)]
    public struct IntCompo3 : IComponentData, IEnableableComponent
    {
        [GhostField] public int Value;
    }

    [GhostEnabledBit]
    [GhostComponent(SendDataForChildEntity = true)]
    public struct FloatCompo1 : IComponentData, IEnableableComponent
    {
        [GhostField(Quantization = 1000)] public float Value;
    }

    [GhostEnabledBit]
    [GhostComponent(SendDataForChildEntity = true)]
    public struct FloatCompo2 : IComponentData, IEnableableComponent
    {
        [GhostField(Quantization = 1000)] public float Value;
    }

    [GhostEnabledBit]
    [GhostComponent(SendDataForChildEntity = true)]
    public struct FloatCompo3 : IComponentData, IEnableableComponent
    {
        [GhostField(Quantization = 1000)] public float Value;
    }

    [GhostComponent(SendDataForChildEntity = true)]
    public struct Buf1 : IBufferElementData
    {
        [GhostField] public int Value;
    }

    [GhostComponent(SendDataForChildEntity = true)]
    public struct Buf2 : IBufferElementData
    {
        [GhostField] public float Value;
    }

    [GhostComponent(SendDataForChildEntity = true)]
    public struct Buf3 : IBufferElementData
    {
        [GhostField] public int Value1;
        [GhostField] public int Value2;
        [GhostField] public float Value3;
        [GhostField] public float Value4;
    }

    [GhostComponent(SendTypeOptimization = GhostSendType.OnlyInterpolatedClients)]
    public struct InterpolatedOnlyComp : IComponentData
    {
        [GhostField] public int Value1;
        [GhostField] public int Value2;
        [GhostField] public int Value3;
        [GhostField] public int Value4;
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct OwnerOnlyComp : IComponentData
    {
        [GhostField] public int Value1;
        [GhostField] public int Value2;
        [GhostField] public int Value3;
        [GhostField] public int Value4;
    }
}
