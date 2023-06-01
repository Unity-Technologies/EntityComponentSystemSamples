using Unity.Entities;
using Unity.Mathematics;

internal struct BoneTag : IComponentData { }

internal struct RootTag : IComponentData { }

internal struct BoneEntity : IBufferElementData
{
    public Entity Value;
}

internal struct RootEntity : IComponentData
{
    public Entity Value;
}

internal struct BindPose : IBufferElementData
{
    public float4x4 Value;
}

internal struct AnimateBlendShape : IComponentData
{
    public float From;
    public float To;
    public float Frequency;
    public float PhaseShift;
}

internal struct AnimateRotation : IComponentData
{
    public quaternion From;
    public quaternion To;
    public float Frequency;
    public float PhaseShift;
}

internal struct AnimateScale : IComponentData
{
    public float3 From;
    public float3 To;
    public float Frequency;
    public float PhaseShift;
}

internal struct AnimatePosition : IComponentData
{
    public float3 From;
    public float3 To;
    public float Frequency;
    public float PhaseShift;
}
