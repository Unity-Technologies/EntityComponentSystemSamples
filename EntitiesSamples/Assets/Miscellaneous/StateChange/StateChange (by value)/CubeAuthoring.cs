using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Miscellaneous.StateChangeValue
{
    public class CubeAuthoring : MonoBehaviour
    {
        public class CubeBaker : Baker<CubeAuthoring>
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
        public bool IsSpinning;
    }
}
