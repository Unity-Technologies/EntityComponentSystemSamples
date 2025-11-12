using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KickBall
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct PlayerInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var moveAction = InputSystem.actions.FindAction("Move");
            moveAction.Enable();
        }

        public void OnUpdate(ref SystemState state)
        {
            var moveAction = InputSystem.actions.FindAction("Move");
            var moveValue = moveAction.ReadValue<Vector2>();
                
            // WithAll<GhostOwnerIsLocal> so that we only modify the input buffer of the local client, not other clients
            // (it's possible and sometimes useful for clients to receive copies of each other's input buffers, but even in
            // those cases we wouldn't want to modify the copies of other players' input buffers)
            foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                input.ValueRW = default;

                // var moveAction = InputSystem.actions.FindAction("Move");
                // var moveValue = moveAction.ReadValue<Vector2>();
                //
                // Debug.Log(moveValue);
                
                input.ValueRW.Horizontal = moveValue.x;
                input.ValueRW.Vertical = moveValue.y;

                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.spaceKey.wasPressedThisFrame)
                    {
                        input.ValueRW.KickBall.Set();
                    }
                    if (keyboard.enterKey.wasPressedThisFrame)
                    {
                        input.ValueRW.SpawnBall.Set();
                    }
                }
            }
        }
    }
}
