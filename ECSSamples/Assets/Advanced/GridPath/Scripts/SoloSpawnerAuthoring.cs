using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[AddComponentMenu("DOTS Samples/GridPath/Solo Spawner")]
public class SoloSpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float CoolDownSeconds;
    [Range(0, 64 * 1024)]
    public int GenerateMaxCount;

    class Baker : Baker<SoloSpawnerAuthoring>
    {
        public override void Bake(SoloSpawnerAuthoring authoring)
        {
            var entity = GetEntity();
            AddComponent( new SoloSpawner
            {
                Prefab = GetEntity(authoring.Prefab),
                CoolDownSeconds = authoring.CoolDownSeconds,
                SecondsUntilGenerate = 0.0f,
                GenerateMaxCount = authoring.GenerateMaxCount,
                GeneratedCount = 0,
                Random = new Random(0xDBC19 * (uint)entity.Index)
            });
        }
    }
}
