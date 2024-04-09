using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;

namespace Streaming.SceneManagement.StreamingVolume
{
#if UNITY_EDITOR
    public class VolumeSetAuthoring : MonoBehaviour
    {
        public List<VolumeAuthoring> volumes;

        public class Baker : Baker<VolumeSetAuthoring>
        {
            public override void Bake(VolumeSetAuthoring authoring)
            {
                var subScene = GetComponent<SubScene>();
                if (subScene != null && authoring.volumes != null && authoring.volumes.Count > 0)
                {
                    var sceneEntity = CreateAdditionalEntity(TransformUsageFlags.None, false,
                        $"{subScene.SceneAsset.name}_Loader");
                    AddComponent(sceneEntity, new LevelInfo
                    {
                        sceneReference = new EntitySceneReference(subScene.SceneAsset)
                    });

                    AddComponent(sceneEntity, new StreamingGO
                    {
                        InstanceID = authoring.gameObject.GetInstanceID()
                    });

                    var buffer = AddBuffer<VolumeBuffer>(sceneEntity);
                    foreach (var volume in authoring.volumes)
                    {
                        var entity = GetEntity(volume, TransformUsageFlags.None);
                        if (entity != Entity.Null)
                        {
                            buffer.Add(new VolumeBuffer
                            {
                                volumeEntity = entity
                            });
                        }
                    }
                }
            }
        }
    }
#endif

    public struct LevelInfo : IComponentData
    {
        public EntitySceneReference sceneReference;
        public Entity runtimeEntity;
    }

    public struct VolumeBuffer : IBufferElementData
    {
        public Entity volumeEntity;
    }
}
