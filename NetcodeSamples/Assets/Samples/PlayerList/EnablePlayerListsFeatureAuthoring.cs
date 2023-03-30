using System;
using Unity.Entities;
using UnityEngine;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>Singleton component enabling the PlayerLists feature, allowing players to query who else is connected to the server.</summary>
    public struct EnablePlayerListsFeature : IComponentData
    {
        /// <inheritdoc cref="PlayerListNotificationBuffer" />
        /// <remarks>Set to -1 to disable this feature.</remarks>
        public double EventListEntryDurationSeconds;
    }

    [DisallowMultipleComponent]
    public class EnablePlayerListsFeatureAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(EnablePlayerListsFeature), "EventListEntryDurationSeconds")]
        public double EventListEntryDurationSeconds;

        class EnablePlayerListsFeatureBaker : Baker<EnablePlayerListsFeatureAuthoring>
        {
            public override void Bake(EnablePlayerListsFeatureAuthoring authoring)
            {
                EnablePlayerListsFeature component = default(EnablePlayerListsFeature);
                component.EventListEntryDurationSeconds = authoring.EventListEntryDurationSeconds;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
