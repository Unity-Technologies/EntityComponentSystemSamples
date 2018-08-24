using Unity.Entities;

namespace Samples.Dungeon.First
{
    public struct WallTile : IComponentData
    {
    }

    public class WallTileComponent : ComponentDataWrapper<WallTile> { }
}
