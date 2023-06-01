using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_URPMySmoothness")]
    struct URPMySmoothnessFloatOverride : IComponentData
    {
        public float Value;
    }
}
