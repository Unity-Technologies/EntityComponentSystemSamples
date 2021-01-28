using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HDRPMyMetallic", MaterialPropertyFormat.Float)]
    struct HDRPMyMetallicFloatOverride : IComponentData
    {
        public float Value;
    }
}
