using Streaming.SceneManagement.Common;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Streaming.SceneManagement.StreamingVolume
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float MovementSpeedMetersPerSecond = 5.0f;
        public Vector3 CameraOffset;

        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Player
                {
                    Speed = authoring.MovementSpeedMetersPerSecond,
                    CameraOffset = authoring.CameraOffset
                });

                // In the "Complete" sample, tiles closer to the relevant entities are loaded at higher LODs.
                AddComponent<Relevant>(entity);
            }
        }
    }

    struct Player : IComponentData
    {
        public float Speed; // meters per second
        public float3 CameraOffset;
    }
}
