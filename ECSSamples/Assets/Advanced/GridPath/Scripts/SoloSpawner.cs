using Unity.Entities;
using Unity.Mathematics;

public struct SoloSpawner : IComponentData
{
    public Entity Prefab;
    public float CoolDownSeconds;
    public float SecondsUntilGenerate;
    public int GenerateMaxCount;
    
    public int GeneratedCount;
    public Random Random;
}
