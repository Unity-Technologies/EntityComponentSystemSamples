using Unity.Entities;

namespace Samples.Dungeon.First
{
    public struct OuterWallTile : IComponentData
    {
    }

    public class OuterWallTileComponent : ComponentDataWrapper<OuterWallTile> { }
}
