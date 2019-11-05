using Unity.Entities;

// ReSharper disable once InconsistentNaming
public struct Spawner_HybridComponent : IComponentData
{
    public Entity prefab;
    public float timeToNextSpawnInSeconds;
}
