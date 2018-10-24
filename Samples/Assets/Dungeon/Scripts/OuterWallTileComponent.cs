using System;
using Unity.Entities;

namespace Samples.Dungeon.First
{
    [Serializable]
    public struct OuterWallTile : IComponentData
    {
    }

    [UnityEngine.DisallowMultipleComponent]
    public class OuterWallTileComponent : ComponentDataWrapper<OuterWallTile> { }
}
