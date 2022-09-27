using Unity.Entities;

public struct CartesianGridOnCubeGenerator : IComponentData
{
    public int RowCount;
    public Entity WallPrefab;
    public float WallSProbability;
    public float WallWProbability;
}

public struct CartesianGridOnCubeGeneratorFloorPrefab : IBufferElementData
{
    public Entity Value;

    public static implicit operator CartesianGridOnCubeGeneratorFloorPrefab(Entity entity) => new() { Value = entity };
    public static implicit operator Entity(CartesianGridOnCubeGeneratorFloorPrefab element) => element.Value;
}
