using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

#if UNITY_EDITOR
public class TestSceneReferenceAuthoring : MonoBehaviour
{
    public SceneAsset _SceneAsset;

    class Baker : Baker<TestSceneReferenceAuthoring>
    {
        public override void Bake(TestSceneReferenceAuthoring authoring)
        {
            AddComponent( new TestSceneReference {SceneReference = new EntitySceneReference(authoring._SceneAsset)});
        }
    }
}

#endif

