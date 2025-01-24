using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KickBall
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct PlayerInputSystem : ISystem
    {
        public class InputActions : IComponentData
        {
            public InputAction MoveAction;
        }
        
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentObject(state.SystemHandle, new InputActions
            {
                MoveAction = InputSystem.actions.FindAction("Move"),
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            // WithAll<GhostOwnerIsLocal> so that we only modify the input buffer of the local client, not other clients
            // (it's possible and sometimes useful for clients to receive copies of each other's input buffers, but even in
            // those cases we wouldn't want to modify the copies of other players' input buffers)
            foreach (var input in
                     SystemAPI.Query<RefRW<PlayerInput>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                input.ValueRW = default;
                
                var actions = state.EntityManager.GetComponentObject<InputActions>(state.SystemHandle);
                var move = actions.MoveAction.ReadValue<Vector2>();
                
                input.ValueRW.Horizontal = move.x;
                input.ValueRW.Vertical = move.y;

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
