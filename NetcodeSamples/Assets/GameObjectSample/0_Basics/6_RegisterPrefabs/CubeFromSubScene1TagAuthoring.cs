using Unity.Entities;
using UnityEngine;

namespace GameObjectSample
{
    public struct CubeFromSubScene1Tag : IComponentData
    {
    }

    [DisallowMultipleComponent]
    public class CubeFromSubScene1TagAuthoring : MonoBehaviour
    {
        class Baker : Baker<CubeFromSubScene1TagAuthoring>
        {
            public override void Bake(CubeFromSubScene1TagAuthoring authoring)
            {
                CubeFromSubScene1Tag component = default;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
