using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace StateMachineValue
{
    [BurstCompile]
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<Hit>(entity);

            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        // This method cannot be Burst compiled because it accesses a managed object (the Camera).
        public void OnUpdate(ref SystemState state)
        {
            var hit = SystemAPI.GetSingleton<Hit>().Value;

            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (new Plane(Vector3.up, 0f).Raycast(ray, out var enter))
                {
                    hit = ray.GetPoint(enter);
                    SystemAPI.SetSingleton(new Hit { Value = hit, ChangedThisFrame = true});
                }
            }
            else
            {
                SystemAPI.SetSingleton(new Hit { Value = hit, ChangedThisFrame = false});
            }
        }
    }
}