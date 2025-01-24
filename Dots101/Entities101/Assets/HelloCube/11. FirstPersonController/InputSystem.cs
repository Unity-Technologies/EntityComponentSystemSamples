using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace HelloCube.FirstPersonController
{
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteFirstPersonController>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var inputState = ref SystemAPI.GetSingletonRW<InputState>().ValueRW;
            inputState.Horizontal = Input.GetAxisRaw("Horizontal");
            inputState.Vertical = Input.GetAxisRaw("Vertical");
            inputState.MouseX = Input.GetAxisRaw("Mouse X");
            inputState.MouseY = Input.GetAxisRaw("Mouse Y");
            inputState.Space = Input.GetKeyDown(KeyCode.Space);
        }
    }
}
