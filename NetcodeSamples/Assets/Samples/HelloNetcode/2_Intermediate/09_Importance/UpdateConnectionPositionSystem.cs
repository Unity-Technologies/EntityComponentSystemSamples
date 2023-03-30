using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct UpdateConnectionPositionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var grid = state.EntityManager.CreateEntity();
            var m_ScaleFunctionPointer = GhostDistanceImportance.ScaleFunctionPointer;
            state.EntityManager.SetName(grid, "GhostImportanceSingleton");
            state.EntityManager.AddComponentData(grid, new GhostDistanceData
            {
                TileSize = new int3(5, 5, 5),
                TileCenter = new int3(0, 0, 0),
                TileBorderWidth = new float3(1f, 1f, 1f),
            });
            state.EntityManager.AddComponentData(grid, new GhostImportance
            {
                ScaleImportanceFunction = m_ScaleFunctionPointer,
                GhostConnectionComponentType = ComponentType.ReadOnly<GhostConnectionPosition>(),
                GhostImportanceDataType = ComponentType.ReadOnly<GhostDistanceData>(),
                GhostImportancePerChunkDataType = ComponentType.ReadOnly<GhostDistancePartitionShared>(),
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<NetworkId>().WithNone<GhostConnectionPosition>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new GhostConnectionPosition
                {
                    Position = new float3(),
                });
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
