using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Miscellaneous.FirstPersonController
{
    [UpdateAfter(typeof(InputSystem))]
    public partial struct ControllerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputState>();
            state.RequireForUpdate<Execute.FirstPersonController>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingleton<InputState>();

            foreach (var (transform, controller) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<Controller>>())
            {
                // Move around with WASD
                var move = new float3(input.Horizontal, 0, input.Vertical);
                move = move * controller.ValueRO.player_speed * SystemAPI.Time.DeltaTime;
                move = math.mul(transform.ValueRO.Rotation, move);

                // Fall down / gravity
                controller.ValueRW.vertical_speed -= 10.0f * SystemAPI.Time.DeltaTime;
                controller.ValueRW.vertical_speed = math.max(-10.0f, controller.ValueRO.vertical_speed);
                move.y = controller.ValueRO.vertical_speed * SystemAPI.Time.DeltaTime;

                transform.ValueRW.Position += move;
                if (transform.ValueRO.Position.y < 0)
                {
                    transform.ValueRW.Position *= new float3(1, 0, 1);
                }

                // Turn player
                var turnPlayer = input.MouseX * controller.ValueRO.mouse_sensitivity * SystemAPI.Time.DeltaTime;
                transform.ValueRW = transform.ValueRO.RotateY(turnPlayer);

                // Camera look up/down
                var turnCam = -input.MouseY * controller.ValueRO.mouse_sensitivity * SystemAPI.Time.DeltaTime;
                controller.ValueRW.camera_pitch += turnCam;

                // Jump
                if (input.Space)
                {
                    controller.ValueRW.vertical_speed = controller.ValueRO.jump_speed;
                }
            }
        }
    }
}
