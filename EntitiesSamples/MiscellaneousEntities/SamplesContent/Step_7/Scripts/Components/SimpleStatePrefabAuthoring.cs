using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace StateChange
{
    public class SimpleStatePrefabAuthoring : MonoBehaviour
    {
    }

    public class SimpleStatePrefabBaker : Baker<SimpleStatePrefabAuthoring>
    {
        public override void Bake(SimpleStatePrefabAuthoring authoring)
        {
            AddComponent(new URPMaterialPropertyBaseColor { Value = (Vector4)Color.red });
        }
    }
}