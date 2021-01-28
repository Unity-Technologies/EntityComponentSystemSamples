using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_URPMySmoothness", MaterialPropertyFormat.Float)]
    struct URPMySmoothnessFloatOverride : IComponentData
    {
        public float Value;
    }
}
