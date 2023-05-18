using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;
namespace Unity.NetCode.Samples.Common
{
    /// <summary>
    ///     Every NetworkId has its own unique Debug color. This system sets it.
    /// </summary>
    [AlwaysSynchronizeSystem]
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    public partial class SetPlayerToDebugColorSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<SetPlayerToDebugColor>().WithChangeFilter<MaterialMeshInfo, GhostOwner>().ForEach((ref URPMaterialPropertyBaseColor color, in GhostOwner ghostOwner) =>
            {
                color.Value = NetworkIdDebugColorUtility.Get(ghostOwner.NetworkId);
            }).Run();
        }
    }
}
