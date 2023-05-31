using System;
using AutoAuthoring;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Baking.AutoAuthoring
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class ManagedSpawnerAuthoring : ManagedAutoAuthoring<ManagedSpawner>
    {
        // Defining OnEnable() makes the inspector show the enabled component checkbox.
        // Disabled components are not baked.
        void OnEnable() { }
    }

    [Serializable]
    public class ManagedSpawner : IComponentData
    {
        public Material Material;
        public Entity Prefab;
        public int InstanceCount = 5;
        public float3 Offset = new float3(2, 0, 2);
    }
#endif // !UNITY_DISABLE_MANAGED_COMPONENTS
}

