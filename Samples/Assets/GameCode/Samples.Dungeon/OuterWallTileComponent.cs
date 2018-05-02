using Unity.Entities;

namespace Samples.Dungeon
{
    public struct OuterWallTile : IComponentData
    {
    }

    public class OuterWallTileComponent : ComponentDataWrapper<OuterWallTile> { }
}
