using Unity.Entities;
using UnityEngine;

namespace GameObjectSample
{
    public struct CubeFromSubScene2Tag : IComponentData
    {
    }

    [DisallowMultipleComponent]
    public class CubeFromSubScene2TagAuthoring : MonoBehaviour
    {
        class Baker : Baker<CubeFromSubScene2TagAuthoring>
        {
            public override void Bake(CubeFromSubScene2TagAuthoring authoring)
            {
                CubeFromSubScene2Tag component = default;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
