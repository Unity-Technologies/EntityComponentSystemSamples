using Unity.Entities;

namespace Samples.Dungeon
{
    public struct FloorTile : IComponentData
    {
    }

    public class FloorTileComponent : ComponentDataWrapper<FloorTile> { }
}
