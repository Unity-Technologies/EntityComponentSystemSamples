using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_URPMyMetallic")]
    struct URPMyMetallicFloatOverride : IComponentData
    {
        public float Value;
    }
}
