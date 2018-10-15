using System;
using Unity.Entities;

namespace Samples.Dungeon.First
{
    [Serializable]
    public struct WallTile : IComponentData
    {
    }

    [UnityEngine.DisallowMultipleComponent]
    public class WallTileComponent : ComponentDataWrapper<WallTile> { }
}
