using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.FixedTimestepSystem
{
    [Serializable]
    public struct ProjectileSpawnTime : IComponentData
    {
        public float SpawnTime;
    }
}
