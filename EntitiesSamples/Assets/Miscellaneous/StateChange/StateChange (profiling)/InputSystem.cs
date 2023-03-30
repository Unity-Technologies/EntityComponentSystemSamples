using Miscellaneous.Execute;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Miscellaneous.StateChange
{
    [BurstCompile]
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<StateChangeProfiling>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var hit = SystemAPI.GetSingleton<Hit>().Value;

            if (Input.GetMouseButton(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (new Plane(Vector3.up, 0f).Raycast(ray, out var enter))
                {
                    hit = ray.GetPoint(enter);
                    SystemAPI.SetSingleton(new Hit { Value = hit });
                }
            }

            var config = SystemAPI.GetSingleton<Config>();

            const int segments = 20;
            const float torad = 2 * math.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float3 p0 = default;
                float3 p1 = default;
                math.sincos(i * torad, out p0.x, out p0.z);
                math.sincos((i + 1) * torad, out p1.x, out p1.z);
                Debug.DrawLine(hit + p0 * config.Radius, hit + p1 * config.Radius);
            }
        }
    }
}
