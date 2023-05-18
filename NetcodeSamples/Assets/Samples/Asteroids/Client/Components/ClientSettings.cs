using Unity.Entities;

public struct ClientSettings : IComponentData
{
    public int predictionRadius;
    public int predictionRadiusMargin;
}
