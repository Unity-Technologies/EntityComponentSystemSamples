using Streaming.SceneManagement.Common;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Streaming.SceneManagement.CompleteSample
{
    public partial struct MoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var cameraTransform = camera.transform;
            var deltaTime = SystemAPI.Time.DeltaTime;

            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");
            var input = new float2(horizontal, vertical);

            foreach (var (transform, player) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<Player>>()
                         .WithAll<RelevantEntity>())
            {
                transform.ValueRW.Position += new float3(input.x, 0.0f, input.y) * player.ValueRO.Speed * deltaTime;
                transform.ValueRW.Position.y = 3f;
                cameraTransform.position = transform.ValueRW.Position + player.ValueRO.CameraOffset;
                cameraTransform.LookAt(transform.ValueRW.Position);
            }
        }
    }
}
