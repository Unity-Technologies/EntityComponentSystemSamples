using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HDRPMyMetallic")]
    struct HDRPMyMetallicFloatOverride : IComponentData
    {
        public float Value;
    }
}
