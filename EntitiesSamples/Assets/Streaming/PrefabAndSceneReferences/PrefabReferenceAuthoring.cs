using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
namespace Streaming.PrefabAndSceneReferences
{
    public class PrefabReferenceAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        class Baker : Baker<PrefabReferenceAuthoring>
        {
            public override void Bake(PrefabReferenceAuthoring authoring)
            {
                GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic);

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PrefabReference()
                {
                    // The EntityPrefabReferences stores the GUID of the prefab.
                    Value = new EntityPrefabReference(authoring.Prefab)
                });
            }
        }
    }

    struct PrefabReference : IComponentData
    {
        public EntityPrefabReference Value;
    }
}
#endif
