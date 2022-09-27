using Unity.Entities;

public struct CartesianGridOnPlaneGenerator : IComponentData
{
    public int RowCount;
    public int ColCount;
    public Entity WallPrefab;
    public float WallSProbability;
    public float WallWProbability;
}

public struct CartesianGridOnPlaneGeneratorFloorPrefab : IBufferElementData
{
    public Entity Value;

    public static implicit operator CartesianGridOnPlaneGeneratorFloorPrefab(Entity entity) => new() { Value = entity };
    public static implicit operator Entity(CartesianGridOnPlaneGeneratorFloorPrefab element) => element.Value;
}
