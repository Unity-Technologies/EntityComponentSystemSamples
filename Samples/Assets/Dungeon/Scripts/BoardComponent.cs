using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Samples.Dungeon.First
{
    [Serializable]
    public struct Board : IComponentData
    {
        public int      TileSetId;
        public float2   GridStep;
        public int2     GridCount;
        public IntRange NumRooms;          // The range of the number of rooms there can be.
        public IntRange RoomWidth;         // The range of widths rooms can have.
        public IntRange RoomHeight;        // The range of heights rooms can have.
        public IntRange CorridorLength;    // The range of lengths corridors between rooms can have.
    }
    
    [Serializable]
    public struct IntRange
    {
        public int m_Min; // The minimum value in this range.
        public int m_Max; // The maximum value in this range.

        // Constructor to set the values.
        public IntRange(int min, int max)
        {
            m_Min = min;
            m_Max = max;
        }

        // Get a random value from the range.
        public int Random
        {
            get { return UnityEngine.Random.Range(m_Min, m_Max); }
        }
    }

    public class BoardComponent : ComponentDataWrapper<Board> { }
    
}