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
        private EntityQuery m_NetworkIdsWithoutGhostConnectionPositionQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<GhostConnectionPosition>();
            m_NetworkIdsWithoutGhostConnectionPositionQuery = state.EntityManager.CreateEntityQuery(builder);
            // Note: CreateEntityQuery ensures we run this system even if we do not have any entities matching this query (i.e. clients),
            // which allows the EnableImportance flag to still work.
        }

        public void OnUpdate(ref SystemState state)
        {
            // Note: This is handled in OnUpdate as we cannot guarantee that the EnableImportance singleton exists during OnCreate, as it's enabled via a sub-scene.
            var shouldEnableImportanceScaling = SystemAPI.HasSingleton<EnableImportance>();
            var hasEnabledImportanceScaling = SystemAPI.TryGetSingletonEntity<GhostImportance>(out var existingImportanceSingletonEntity);
            if (shouldEnableImportanceScaling != hasEnabledImportanceScaling)
            {
                if (shouldEnableImportanceScaling)
                {
                    var grid = state.EntityManager.CreateSingleton(new GhostDistanceData
                    {
                        TileSize = new int3(5, 5, 5),
                        TileCenter = new int3(0, 0, 0),
                        TileBorderWidth = new float3(1f, 1f, 1f),
                    });
                    state.EntityManager.AddComponentData(grid, new GhostImportance
                    {
                        ScaleImportanceFunction = GhostDistanceImportance.ScaleFunctionPointer,
                        GhostConnectionComponentType = ComponentType.ReadOnly<GhostConnectionPosition>(),
                        GhostImportanceDataType = ComponentType.ReadOnly<GhostDistanceData>(),
                        GhostImportancePerChunkDataType = ComponentType.ReadOnly<GhostDistancePartitionShared>(),
                    });

                    // Note: If you ALWAYS want the "Distance Importance Scaling" feature enabled:
                    // - It is safe to move the above code into OnCreate.
                    // - But, you must delete the EnableImportance component (and Authoring), as you cannot sample it via OnCreate.

                }
                else
                {
                    state.EntityManager.DestroyEntity(existingImportanceSingletonEntity);
                }
            }

            if (shouldEnableImportanceScaling)
            {
                state.EntityManager.AddComponent<GhostConnectionPosition>(m_NetworkIdsWithoutGhostConnectionPositionQuery);
                // Note: In a real game (and assuming your clients character controller moves and/or rotates),
                // you'd need to update your GhostConnectionPosition values every frame.
                // In this sample, the character controller is in a fixed position, which happens to be default(float3).
            }
        }
    }
}
