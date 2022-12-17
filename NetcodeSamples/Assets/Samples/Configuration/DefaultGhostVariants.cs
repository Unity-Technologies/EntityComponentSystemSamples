using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.NetCode.Samples
{
    /// <summary>Registers the default variants for all samples. Since multiple user-defined variants are present for the
    /// Transform components, we must explicitly define a default, and how it applies to components on child entities.</summary>
    [CreateBefore(typeof(TransformDefaultVariantSystem))]
    sealed class DefaultGhostVariantSystem : DefaultVariantSystemBase
    {
        protected override void RegisterDefaultVariants(Dictionary<ComponentType, Rule> defaultVariants)
        {
#if !ENABLE_TRANSFORM_V1
            defaultVariants.Add(typeof(LocalTransform), Rule.OnlyParents(typeof(TransformDefaultVariant)));
#else
            defaultVariants.Add(typeof(Rotation), Rule.OnlyParents(typeof(RotationDefaultVariant)));
            defaultVariants.Add(typeof(Translation), Rule.OnlyParents(typeof(TranslationDefaultVariant)));
#endif
        }
    }
}
