using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_URPMyMetallic", MaterialPropertyFormat.Float)]
    struct URPMyMetallicFloatOverride : IComponentData
    {
        public float Value;
    }
}
