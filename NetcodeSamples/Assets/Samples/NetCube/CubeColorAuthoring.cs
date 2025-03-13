using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// The color of the cube. This is stored in a ghost field so that the color
/// will stay with both the connection and the cube/player itself, even after they have
/// been migrated to a new server.
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct CubeColor : IComponentData
{
    [GhostField]
    public int Value;
}

[DisallowMultipleComponent]
public class CubeColorAuthoring : MonoBehaviour
{
    class Baker : Baker<CubeColorAuthoring>
    {
        public override void Bake(CubeColorAuthoring authoring)
        {
            CubeColor component = default(CubeColor);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
