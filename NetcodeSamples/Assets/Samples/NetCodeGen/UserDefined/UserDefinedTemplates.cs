
using System.Collections.Generic;
namespace Unity.NetCode.Generators
{
    public static partial class UserDefinedTemplates
    {
        static partial void RegisterTemplates(List<TypeRegistryEntry> templates, string defaultRootPath)
        {
            templates.AddRange(new[]{

                new TypeRegistryEntry
                {
                    Type = "Unity.Mathematics.float3",
                    SubType = GhostFieldSubType.Translation2D,
                    Quantized = true,
                    Smoothing = SmoothingAction.InterpolateAndExtrapolate,
                    SupportCommand = false,
                    Composite = false,
#if UNITY_2021_2_OR_NEWER
                    Template = "Custom.Translation2d",
#else
                    Template = "Assets/Samples/NetCodeGen/Templates/Translation2d.NetCodeSourceGenerator.additionalfile",
#endif
                    TemplateOverride = "",
                },
                new TypeRegistryEntry
                {
                    Type = "Unity.Mathematics.quaternion",
                    SubType = GhostFieldSubType.Rotation2D,
                    Quantized = true,
                    Smoothing = SmoothingAction.InterpolateAndExtrapolate,
                    SupportCommand = false,
                    Composite = false,
#if UNITY_2021_2_OR_NEWER
                    Template = "Custom.Rotation2d",
#else
                    Template = "Assets/Samples/NetCodeGen/Templates/Rotation2d.NetCodeSourceGenerator.additionalfile",
#endif
                    TemplateOverride = "",
                },
            });
        }
    }
}
