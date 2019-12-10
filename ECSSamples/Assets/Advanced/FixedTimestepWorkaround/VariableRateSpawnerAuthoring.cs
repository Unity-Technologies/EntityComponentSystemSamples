using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Samples.FixedTimestepSystem.Authoring
{
    [AddComponentMenu("DOTS Samples/FixedTimestepWorkaround/Variable Rate Spawner")]
    [ConverterVersion("joe", 1)]
    public class VariableRateSpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public GameObject projectilePrefab;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var spawnerData = new VariableRateSpawner
            {
                Prefab = conversionSystem.GetPrimaryEntity(projectilePrefab),
            };
            dstManager.AddComponentData(entity, spawnerData);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(projectilePrefab);
        }
    }
}
