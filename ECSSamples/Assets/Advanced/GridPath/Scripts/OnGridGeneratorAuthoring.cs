using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/GridPath/OnGridGenerator")]
[ConverterVersion("joe", 1)]
public class OnGridGeneratorAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject Prefab;
    public float CoolDownSeconds;
    [Range(0,64*1024)]
    public int GenerateMaxCount;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new OnGridGenerator
        {
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            CoolDownSeconds = CoolDownSeconds,
            SecondsUntilGenerate = 0.0f,
            GenerateMaxCount = GenerateMaxCount,
            GeneratedCount = 0,
            Random = new Random(0xDBC19 * (uint)entity.Index )
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(Prefab);
    }
}
