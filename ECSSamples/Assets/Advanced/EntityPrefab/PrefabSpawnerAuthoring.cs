using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

#if UNITY_EDITOR
public class PrefabSpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject[] Prefabs;
    public int SpawnCount;
    public float SpawnsPerSecond;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new PrefabSpawner {SpawnsRemaining = SpawnCount, SpawnsPerSecond = SpawnsPerSecond});
        var buffer = dstManager.AddBuffer<PrefabSpawnerBufferElement>(entity);
        foreach (var prefab in Prefabs)
        {
            buffer.Add(new PrefabSpawnerBufferElement {Prefab = new EntityPrefabReference(prefab)});
        }
    }
}

#endif
