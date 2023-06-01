using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.StateChange
{
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Execute.StateChange>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var hit = SystemAPI.GetSingletonRW<Hit>();
            hit.ValueRW.HitChanged = false;

            if (Camera.main == null || !Input.GetMouseButton(0))
            {
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (new Plane(Vector3.up, 0f).Raycast(ray, out var dist))
            {
                hit.ValueRW.HitChanged = true;
                hit.ValueRW.Value = ray.GetPoint(dist);
            }
        }
    }
}
