using System;
using Unity.Entities;

namespace Samples.Dungeon.First
{
    [Serializable]
    public struct FloorTile : IComponentData
    {
    }

    [UnityEngine.DisallowMultipleComponent]
    public class FloorTileComponent : ComponentDataWrapper<FloorTile> { }
}
