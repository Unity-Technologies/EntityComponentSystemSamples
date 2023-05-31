using Unity.Entities;
using UnityEngine;

namespace Events
{
    public class TriggerVolumeChangeMaterialAuthoring : MonoBehaviour
    {
        public GameObject ReferenceGameObject = null;

        class Baker : Baker<TriggerVolumeChangeMaterialAuthoring>
        {
            public override void Bake(TriggerVolumeChangeMaterialAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TriggerVolumeChangeMaterial
                {
                    ReferenceEntity = GetEntity(authoring.ReferenceGameObject, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public struct TriggerVolumeChangeMaterial : IComponentData
    {
        public Entity ReferenceEntity;
    }
}
