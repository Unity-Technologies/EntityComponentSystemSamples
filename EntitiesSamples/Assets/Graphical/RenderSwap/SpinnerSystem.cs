using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Graphical.RenderSwap
{
    public partial struct SpinnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<ExecuteRenderSwap>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var angle = math.radians((float)SystemAPI.Time.ElapsedTime * 120);
            var axis = float3.zero;
            math.sincos(angle, out axis.x, out axis.z);

            var meshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>();
            var config = SystemAPI.GetSingleton<Config>();
            var meshOn = meshInfoLookup[config.StateOn];
            var meshOff = meshInfoLookup[config.StateOff];

            foreach (var (meshInfo, transform) in
                     SystemAPI.Query<RefRW<MaterialMeshInfo>, RefRO<LocalTransform>>()
                         .WithAll<SpinTile>())
            {
                var pos = transform.ValueRO.Position;
                var closest = math.dot(pos, axis) * axis;
                var sqDist = math.distancesq(closest, pos);

                meshInfo.ValueRW = sqDist < 10f ? meshOn : meshOff;
            }
        }
    }
}
