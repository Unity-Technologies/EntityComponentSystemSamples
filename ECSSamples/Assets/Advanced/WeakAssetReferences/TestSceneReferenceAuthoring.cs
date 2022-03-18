using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

#if UNITY_EDITOR
public class TestSceneReferenceAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public SceneAsset _SceneAsset;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TestSceneReference {SceneReference = new EntitySceneReference(_SceneAsset)});
    }
}

#endif

