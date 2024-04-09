using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEngine;

namespace Streaming.SceneManagement.CompleteSample
{
    public class TileInfoAuthoring : MonoBehaviour
    {
        public int randomSeed;
        public float tileSize; // size of square side
        public int2 minBoundary;
        public int2 maxBoundary;
        public List<TileTemplate> tileTemplates;

#if UNITY_EDITOR
        public class Baker : Baker<TileInfoAuthoring>
        {
            public override void Bake(TileInfoAuthoring authoring)
            {
                // Make a copy to filter entries with null scenes
                List<TileTemplate> tileTemplates =
                    new List<TileTemplate>(authoring.tileTemplates.Count);
                List<EntitySceneReference> sceneReferences =
                    new List<EntitySceneReference>(authoring.tileTemplates.Count);

                // Get the scene references
                foreach (var tileTemplate in authoring.tileTemplates)
                {
                    if (tileTemplate != null)
                    {
                        // We want to create a dependency to the scene in case the scene gets deleted
                        // This needs to be outside the authoring.scene != null check in case the asset file gets deleted and then restored.
                        DependsOn(tileTemplate.tileScene);

                        if (tileTemplate.tileScene != null)
                        {
                            tileTemplates.Add(tileTemplate);
                            sceneReferences.Add(new EntitySceneReference(tileTemplate.tileScene));
                        }
                    }
                }

                int2 min = math.min(authoring.minBoundary, authoring.maxBoundary);
                int2 max = math.max(authoring.minBoundary, authoring.maxBoundary);

                var random = new Unity.Mathematics.Random((uint)authoring.randomSeed);
                float2 tileSize = new float2(authoring.tileSize, authoring.tileSize);

                // Choose the tiles for the world from the patterns
                for (int x = min.x; x <= max.x; ++x)
                {
                    for (int y = min.y; y <= max.y; ++y)
                    {
                        int selectedTile = random.NextInt(0, tileTemplates.Count);

                        var tileEntity = CreateAdditionalEntity(TransformUsageFlags.None, false, $"Tile {x}_{y}");

                        // Store the information to instantiate the scene into the right tile position
                        var loadingDistance = tileTemplates[selectedTile].loadingDistance;
                        var unloadingDistance = tileTemplates[selectedTile].unloadingDistance;
                        AddComponent(tileEntity, new TileInfo
                        {
                            Scene = sceneReferences[selectedTile],
                            Position = tileSize * new float2(x, y),
                            Rotation = random.NextInt(4) * (math.PI / 2f),  // 0, 90, 180, or 270 degrees
                            LoadingDistanceSq = loadingDistance * loadingDistance,
                            UnloadingDistanceSq = unloadingDistance * unloadingDistance
                        });

                        // This component will store the distance to the Relevant entities
                        AddComponent<DistanceToRelevant>(tileEntity);
                    }
                }
            }
        }
#endif

        [Serializable]
        public class TileTemplate
        {
#if UNITY_EDITOR
            public UnityEditor.SceneAsset tileScene; // the scene to instantiate
#endif
            public float loadingDistance; // proximity distance within which to consider loading the scene
            public float unloadingDistance; // proximity distance within which to consider unloading the scene
        }
    }

    public struct TileInfo : IComponentData
    {
        public EntitySceneReference Scene; // scene instance
        public float2 Position;
        public float Rotation;
        public float LoadingDistanceSq;
        public float UnloadingDistanceSq;
    }

    public struct DistanceToRelevant : IComponentData
    {
        public float DistanceSq;
    }
}
