using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// A server only component to keep track of what index values have been assigned to new players for color
/// and position. These need to be stored so they'll be migrated to the new host in the event of
/// host migration. The new host will then continue assigning colors/positions properly in sequence
/// to new players.
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct CubeConfig : IComponentData
{
    [GhostField] public int NextColorValue;
    [GhostField] public int NextPositionValue;
}

[DisallowMultipleComponent]
public class CubeConfigAuthoring : MonoBehaviour
{
    class Baker : Baker<CubeConfigAuthoring>
    {
        public override void Bake(CubeConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new CubeConfig() { NextColorValue = 1, NextPositionValue = 1});
        }
    }
}
