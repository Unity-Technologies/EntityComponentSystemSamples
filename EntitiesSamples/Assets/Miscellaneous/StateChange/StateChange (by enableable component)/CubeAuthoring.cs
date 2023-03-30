using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Miscellaneous.StateChangeEnableable
{
    public class CubeAuthoring : MonoBehaviour
    {
        public class Baker : Baker<CubeAuthoring>
        {
            public override void Bake(CubeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new URPMaterialPropertyBaseColor { Value = (Vector4)Color.white });
                AddComponent<Cube>(entity);
                AddComponent<Spinner>(entity);
            }
        }
    }

    public struct Cube : IComponentData
    {
    }

    public struct Spinner : IComponentData, IEnableableComponent
    {
        // (the component should actually be empty, but this dummy field
        // was added as workaround for a bug in source generation)
        public int Value;
    }
}
