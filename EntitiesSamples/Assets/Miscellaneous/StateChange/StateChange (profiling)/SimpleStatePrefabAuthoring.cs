using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Miscellaneous.StateChange
{
    public class SimpleStatePrefabAuthoring : MonoBehaviour
    {
        class SimpleStatePrefabBaker : Baker<SimpleStatePrefabAuthoring>
        {
            public override void Bake(SimpleStatePrefabAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new URPMaterialPropertyBaseColor { Value = (Vector4)Color.red });
            }
        }
    }
}
