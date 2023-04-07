using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace LearnSample
{
    [AddComponentMenu("Learn Samples/Spawner")]
    [ConverterVersion("yangyang", 1)]
    public class SpawnAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject Prefab;
        public List<Transform> PathPoints;
        public int CountX;
        public int CountY;
        public float SoliderMoveSpeed;

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(Prefab);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponents(entity, new ComponentTypes(
                new ComponentType[]
                {
                    typeof(SpawnComponent),
                    typeof(PathPointComponent)
                }));

            var pathPointBuffer = dstManager.GetBuffer<PathPointComponent>(entity);
            foreach (var pointTrans in PathPoints)
            {
                pathPointBuffer.Add(new PathPointComponent { Value = pointTrans.position });
            }

            var spawnerData = new SpawnComponent
            {
                Prefab = conversionSystem.GetPrimaryEntity(Prefab),
                CountX = CountX,
                CountY = CountY,
                MoveSpeed = SoliderMoveSpeed
            };
            dstManager.AddComponentData(entity, spawnerData);
        }
    }
}
