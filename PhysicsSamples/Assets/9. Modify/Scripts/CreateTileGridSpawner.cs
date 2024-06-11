// The purpose of this demo is to show how to create mesh colliders at runtime. The demo uses trigger events as a way to
// interact during runtime for a collider creation event to take place.
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics
{
    public class CreateTileGridSpawner : MonoBehaviour
    {
        public GameObject GridPrefab;
        public GameObject WallPrefab;

        class CreateTileGridBaker : Baker<CreateTileGridSpawner>
        {
            public override void Bake(CreateTileGridSpawner authoring)
            {
                DependsOn(authoring.GridPrefab);
                if (authoring.GridPrefab == null || authoring.WallPrefab == null) return;
                var prefabGridEntity = GetEntity(authoring.GridPrefab, TransformUsageFlags.Dynamic);
                var prefabWallEntity = GetEntity(authoring.WallPrefab, TransformUsageFlags.Dynamic);

                var createComponent = new CreateTileGridSpawnerComponent
                {
                    GridEntity = prefabGridEntity,
                    WallEntity = prefabWallEntity,
                    SpawningPosition = authoring.transform.position
                };
                var gridSpawnerEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(gridSpawnerEntity, createComponent);
            }
        }
    }

    public struct CreateTileGridSpawnerComponent : IComponentData
    {
        public Entity GridEntity;
        public Entity WallEntity;
        public float3 SpawningPosition;
    }

    public struct WallsTagComponent : IComponentData
    {
    }
}
