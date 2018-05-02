using System;
using Unity.Entities;
using UnityEngine;

namespace Samples.Dungeon
{
    [Serializable]
    public struct BoardReference : ISharedComponentData
    {
        public int TileSetId;
    }
    public class BoardReferenceComponent : SharedComponentDataWrapper<BoardReference> { }
}
