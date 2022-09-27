using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

#if UNITY_EDITOR
public class TestPrefabReferenceAuthoring : MonoBehaviour
{
    public GameObject _Prefab;
    class Baker : Baker<TestPrefabReferenceAuthoring>
    {
        public override void Bake(TestPrefabReferenceAuthoring authoring)
        {
            GetEntity(authoring._Prefab);
        }
    }
}

#endif

