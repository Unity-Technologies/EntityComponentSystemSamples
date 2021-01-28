using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HDRPMySmoothness", MaterialPropertyFormat.Float)]
    struct HDRPMySmoothnessFloatOverride : IComponentData
    {
        public float Value;
    }
}
