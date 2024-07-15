using System;
using Unity.Entities;
using Unity.Rendering;

namespace Unity.NetCode.Samples.Common
{
    /// <summary>Denotes that a ghost will be set to the debug color specified in <see cref="NetworkIdDebugColorUtility"/>.</summary>
    public struct SetPlayerToDebugColor : IComponentData
    {
    }

    [UnityEngine.DisallowMultipleComponent]
    public class SetPlayerToDebugColorAuthoring : UnityEngine.MonoBehaviour
    {
        class SetPlayerToDebugColorBaker : Baker<SetPlayerToDebugColorAuthoring>
        {
            public override void Bake(SetPlayerToDebugColorAuthoring authoring)
            {
                SetPlayerToDebugColor component = default(SetPlayerToDebugColor);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
                AddComponent(entity, new URPMaterialPropertyBaseColor {Value = 1});
            }
        }
    }
}
