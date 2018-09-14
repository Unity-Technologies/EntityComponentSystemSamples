using System;
using Unity.Entities;

namespace Samples.Dungeon.First
{
    [Serializable]
    public struct WallTile : IComponentData
    {
    }

    public class WallTileComponent : ComponentDataWrapper<WallTile> { }
}
