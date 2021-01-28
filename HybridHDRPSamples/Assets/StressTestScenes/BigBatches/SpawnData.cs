using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct SpawnData : IComponentData
{
    public Entity Prefab;
    public int CountX;
    public int CountY;
    public bool HasRenderingDisabledEntities;
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class SpawnData : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public GameObject Prefab;
        public int CountX;
        public int CountY;
        public bool HasRenderingDisabledEntities;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new global::SpawnData
            {
                Prefab = conversionSystem.GetPrimaryEntity(Prefab),
                CountX = CountX,
                CountY = CountY,
                HasRenderingDisabledEntities = HasRenderingDisabledEntities,
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(Prefab);
        }

    }
}