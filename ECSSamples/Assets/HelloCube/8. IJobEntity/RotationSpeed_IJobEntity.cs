using Unity.Entities;

[GenerateAuthoringComponent]
public struct RotationSpeed_IJobEntity : IComponentData
{
    public float RadiansPerSecond;
}
