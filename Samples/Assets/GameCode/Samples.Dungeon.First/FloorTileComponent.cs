using Unity.Entities;

namespace Samples.Dungeon.First
{
    public struct FloorTile : IComponentData
    {
    }

    public class FloorTileComponent : ComponentDataWrapper<FloorTile> { }
}
