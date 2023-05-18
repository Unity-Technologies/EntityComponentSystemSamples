using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Miscellaneous.StateChangeStructural
{
    public class CubeAuthoring : MonoBehaviour
    {
        class CubeBaker : Baker<CubeAuthoring>
        {
            public override void Bake(CubeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new URPMaterialPropertyBaseColor { Value = (Vector4)Color.white });
                AddComponent<Cube>(entity);
            }
        }
    }

    public struct Cube : IComponentData
    {
    }

    // Added at runtime, not in baking.
    public struct Spinner : IComponentData
    {
    }
}
