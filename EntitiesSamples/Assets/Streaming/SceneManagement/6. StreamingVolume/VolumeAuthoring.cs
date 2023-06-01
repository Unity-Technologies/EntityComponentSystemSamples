using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Streaming.SceneManagement.StreamingVolume
{
    public class VolumeAuthoring : MonoBehaviour
    {
        public class Baker : Baker<VolumeAuthoring>
        {
            public override void Bake(VolumeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new Volume
                {
                    Scale = GetComponent<Transform>().localScale
                });

               AddComponent(entity, new StreamingGO
               {
                    InstanceID = authoring.gameObject.GetInstanceID()
               });
            }
        }
    }

    public struct Volume : IComponentData
    {
        public float3 Scale;
    }

    public struct StreamingGO : IComponentData
    {
        public int InstanceID;
    }
}
