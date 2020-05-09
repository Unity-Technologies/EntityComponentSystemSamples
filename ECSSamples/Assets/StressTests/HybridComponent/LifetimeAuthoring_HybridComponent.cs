using Unity.Entities;
using UnityEngine;

// ReSharper disable once InconsistentNaming
[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/HybridComponent/Lifetime")]
[ConverterVersion("joe", 1)]
public class LifetimeAuthoring_HybridComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public float timeRemainingInSeconds = 12;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Lifetime_HybridComponent { timeRemainingInSeconds = timeRemainingInSeconds });
    }
}
