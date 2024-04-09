using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Graphical.PrefabInitializer
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct WireframeRenderingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteWireframeBlob>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (local, world) in
                     SystemAPI.Query<RefRO<WireframeLocalSpace>, DynamicBuffer<WireframeWorldSpace>>())
            {
                var vertices = world.Reinterpret<float3>();
                ref var segments = ref local.ValueRO.Blob.Value.Segments;
                for (int i = 0; i < segments.Length; i++)
                {
                    Debug.DrawLine(vertices[segments[i].x], vertices[segments[i].y]);
                }
            }
        }
    }
}
