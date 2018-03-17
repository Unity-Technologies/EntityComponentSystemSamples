using System;
using Unity.Entities;
using UnityEngine;

namespace Samples.Common
{
    [Serializable]
    public struct SpawnRandomInSphere : ISharedComponentData
    {
        public GameObject prefab;
        public float radius;
        public int count;
    }

    public class SpawnRandomInSphereComponent : SharedComponentDataWrapper<SpawnRandomInSphere> { }
}
