using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

#if UNITY_EDITOR
public class PrefabSpawnerAuthoring : MonoBehaviour
{
    public GameObject[] Prefabs;
    public int SpawnCount;
    public float SpawnsPerSecond;

    class Baker : Baker<PrefabSpawnerAuthoring>
    {
        public override void Bake(PrefabSpawnerAuthoring authoring)
        {
            AddComponent( new PrefabSpawner {SpawnsRemaining = authoring.SpawnCount, SpawnsPerSecond = authoring.SpawnsPerSecond});
            var buffer = AddBuffer<PrefabSpawnerBufferElement>();
            foreach (var prefab in authoring.Prefabs)
            {
                buffer.Add(new PrefabSpawnerBufferElement {Prefab = new EntityPrefabReference(prefab)});
            }
        }
    }
}

#endif
