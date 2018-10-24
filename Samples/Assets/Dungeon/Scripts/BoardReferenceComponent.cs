﻿using System;
using Unity.Entities;

namespace Samples.Dungeon.First
{
    [Serializable]
    public struct BoardReference : ISharedComponentData
    {
        public int TileSetId;
    }
    public class BoardReferenceComponent : SharedComponentDataWrapper<BoardReference> { }
}
