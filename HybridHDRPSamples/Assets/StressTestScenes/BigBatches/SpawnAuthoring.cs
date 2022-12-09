using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct SpawnData : IComponentData
{
    public Entity Prefab;
    public int CountX;
    public int CountY;
    public bool HasRenderingDisabledEntities;
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class SpawnAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public int CountX;
        public int CountY;
        public bool HasRenderingDisabledEntities;

        class Baker : Baker<SpawnAuthoring>
        {
            public override void Bake(SpawnAuthoring authoring)
            {
                var spawnerData = new SpawnData
                {
                    Prefab = GetEntity(authoring.Prefab),
                    CountX = authoring.CountX,
                    CountY = authoring.CountY,
                    HasRenderingDisabledEntities = authoring.HasRenderingDisabledEntities
                };
                AddComponent(spawnerData);
            }
        }
    }
}