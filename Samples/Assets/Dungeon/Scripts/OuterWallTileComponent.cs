using System;
using Unity.Entities;

namespace Samples.Dungeon.First
{
    [Serializable]
    public struct OuterWallTile : IComponentData
    {
    }

    public class OuterWallTileComponent : ComponentDataWrapper<OuterWallTile> { }
}
