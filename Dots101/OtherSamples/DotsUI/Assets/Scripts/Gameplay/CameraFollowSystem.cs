using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.DotsUISample
{
    // Update after physics so we can get the accurate player position.
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CameraFollowSystem : ISystem
    {
        private bool cameraInit;

        public void OnUpdate(ref SystemState state)
        {
            if (!cameraInit)
            {
                var camera = GameObject.FindFirstObjectByType<Camera>();
                if (camera == null)
                {
                    return;
                }

                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new CameraRef
                {
                    Offset = new float3(0, 10f, -10f),
                    Camera = camera,
                });

                cameraInit = true;
            }

            var cameraRef = SystemAPI.GetSingleton<CameraRef>();
            if (!cameraRef.Camera.IsValid())
            {
                return;
            }

            foreach (var playerTransform in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<Player>())
            {
                var camera = cameraRef.Camera.Value;
                var targetPos = playerTransform.ValueRO.Position + cameraRef.Offset;

                const float sharpness = 10;
                var t = math.saturate(1f - math.exp(-sharpness * SystemAPI.Time.DeltaTime));

                camera.transform.position = (Vector3)math.lerp(camera.transform.position, targetPos, t);
            }
        }
    }
}