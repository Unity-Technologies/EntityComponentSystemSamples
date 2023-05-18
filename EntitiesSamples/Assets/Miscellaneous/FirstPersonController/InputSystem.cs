using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.FirstPersonController
{
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton<InputState>();
            state.RequireForUpdate<Execute.FirstPersonController>();
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

    public struct InputState : IComponentData
    {
        public float Horizontal;
        public float Vertical;
        public float MouseX;
        public float MouseY;
        public bool Space;
    }
}
