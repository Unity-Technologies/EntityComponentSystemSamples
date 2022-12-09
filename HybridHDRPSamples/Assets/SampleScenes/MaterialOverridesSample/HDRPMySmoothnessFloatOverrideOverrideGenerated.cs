using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HDRPMySmoothness")]
    struct HDRPMySmoothnessFloatOverride : IComponentData
    {
        public float Value;
    }
}
