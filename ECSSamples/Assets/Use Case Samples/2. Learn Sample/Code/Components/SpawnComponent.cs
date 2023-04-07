using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace LearnSample
{
    public struct SpawnComponent : IComponentData
    {
        public Entity Prefab;
        public int CountX;
        public int CountY;
        public float MoveSpeed;
    }
}