using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_URPMyColor")]
    struct URPMyColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
