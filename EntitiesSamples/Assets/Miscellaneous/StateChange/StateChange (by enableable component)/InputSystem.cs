using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.StateChangeEnableable
{
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Execute.StateChangeEnableable>();
        }

        // This method cannot be Burst compiled because it accesses a managed object (the Camera).
        public void OnUpdate(ref SystemState state)
        {
            var hit = SystemAPI.GetSingleton<Hit>().Value;

            if (Camera.main != null && Input.GetMouseButtonDown(0))
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
