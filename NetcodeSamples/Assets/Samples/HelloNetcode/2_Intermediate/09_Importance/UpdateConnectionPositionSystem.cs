using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
            state.RequireForUpdate<EnableImportance>();
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
            var enableImportance = SystemAPI.GetSingleton<EnableImportance>();
            var hasEnabledImportanceScaling = SystemAPI.TryGetSingletonEntity<GhostImportance>(out var existingImportanceSingletonEntity);
            if (enableImportance.Enabled != hasEnabledImportanceScaling)
            {
                if (enableImportance.Enabled)
                {
                    var grid = state.EntityManager.CreateSingleton(enableImportance.TilingConfiguration);
                    state.EntityManager.AddComponentData(grid, new GhostImportance
                    {
                        BatchScaleImportanceFunction = enableImportance.UseBatchedImportanceFunction ? GhostDistanceImportance.BatchScaleFunctionPointer : default,
                        GhostConnectionComponentType = ComponentType.ReadOnly<GhostConnectionPosition>(),
                        GhostImportanceDataType = ComponentType.ReadOnly<GhostDistanceData>(),
                        GhostImportancePerChunkDataType = ComponentType.ReadOnly<GhostDistancePartitionShared>(),
                    });

                    // Note: If you ALWAYS want the "Distance Importance Scaling" feature enabled:
                    // - It is safe to move the above code into OnCreate.
                    // - But, you must delete the EnableImportance component (and Authoring), as you cannot sample it via OnCreate
                    // (as the sub scene will not be loaded yet, in builds).

                    // Disable the automatic adding of the importance shared component.
                    GhostDistancePartitioningSystem.AutomaticallyAddGhostDistancePartitionSharedComponent = false;
                }
                else
                {
                    state.EntityManager.DestroyEntity(existingImportanceSingletonEntity);
                    GhostDistancePartitioningSystem.AutomaticallyAddGhostDistancePartitionSharedComponent = true;
                }
            }

            if (enableImportance.Enabled)
            {
                state.EntityManager.AddComponent<GhostConnectionPosition>(m_NetworkIdsWithoutGhostConnectionPositionQuery);
                // Note: In a real game (and assuming your clients character controller moves and/or rotates),
                // you'd need to update your GhostConnectionPosition values every frame.
                // In this sample, the character controller is in a fixed position, which happens to be default(float3).
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            GhostDistancePartitioningSystem.AutomaticallyAddGhostDistancePartitionSharedComponent = true;
        }
    }
}
