using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_URPUnlitMyColor")]
    struct URPUnlitMyColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
