using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Graphical.PrefabInitializer
{
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Spawner>();
            state.RequireForUpdate<ExecuteWireframeBlob>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (Camera.main != null && Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (new Plane(Vector3.up, 0f).Raycast(ray, out var enter))
                {
                    var hit = ray.GetPoint(enter);
                    var prefab = SystemAPI.GetSingleton<Spawner>().Prefab;
                    var instance = state.EntityManager.Instantiate(prefab);
                    var transform = SystemAPI.GetComponentRW<LocalTransform>(instance);
                    transform.ValueRW.Position = hit;
                }
            }
        }
    }
}
