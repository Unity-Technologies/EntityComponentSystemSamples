using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

#if UNITY_EDITOR
public class TestPrefabReferenceAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject _Prefab;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TestPrefabReference {PrefabReference = new EntityPrefabReference(_Prefab)});
    }
}

#endif

