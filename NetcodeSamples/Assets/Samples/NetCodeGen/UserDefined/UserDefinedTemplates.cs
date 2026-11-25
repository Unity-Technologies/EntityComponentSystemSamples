
using System.Collections.Generic;
namespace Unity.Netcode.Generators
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
                    Template = "Custom.Translation2d",
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
                    Template = "Custom.Rotation2d",
                    TemplateOverride = "",
                },
                new TypeRegistryEntry
                {
                    Type = "System.Boolean",
                    Quantized = false,
                    Smoothing = SmoothingAction.Clamp,
                    SupportCommand = true,
                    Composite = false,
                    Template = "NetCode.GhostSnapshotValueUInt.cs",
                    TemplateOverride = "PreserializeBoolTest",
                    SubType = GhostFieldSubType.BoolTest
                }
            });
        }
    }
}
