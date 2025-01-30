using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// The next color available to the next player/connection which joins.
/// The color is then assigned to that player for the duration of their session.
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct PlayerColorNext : IComponentData
{
    [GhostField] public int Value;
}
