using Unity.Entities;

namespace Samples.Dungeon
{
    public struct WallTile : IComponentData
    {
    }

    public class WallTileComponent : ComponentDataWrapper<WallTile> { }
}
