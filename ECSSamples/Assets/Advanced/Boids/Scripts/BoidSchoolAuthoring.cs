#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Samples.Boids
{
    [RequiresEntityConversion]
    [AddComponentMenu("DOTS Samples/Boids/BoidSchool")]
    [ConverterVersion("macton", 4)]
    public class BoidSchoolAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public GameObject Prefab;
        public float InitialRadius;
        public int Count;
        
        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(Prefab);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new BoidSchool
            {
                Prefab = conversionSystem.GetPrimaryEntity(Prefab),
                Count = Count,
                InitialRadius = InitialRadius
            });
        }
    }
}

#endif
