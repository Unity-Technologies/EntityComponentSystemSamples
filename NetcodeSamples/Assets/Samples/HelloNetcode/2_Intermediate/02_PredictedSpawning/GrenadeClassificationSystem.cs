using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.LowLevel;

namespace Samples.HelloNetcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostSpawnClassificationSystemGroup))]
    [UpdateAfter(typeof(GhostSpawnClassificationSystem))]
    [CreateAfter(typeof(GhostCollectionSystem))]
    [CreateAfter(typeof(GhostReceiveSystem))]
    [BurstCompile]
    public partial struct GrenadeClassificationSystem : ISystem
    {
        SnapshotDataLookupHelper m_SnapshotDataLookupHelper;
        BufferLookup<PredictedGhostSpawn> m_PredictedGhostSpawnLookup;
        ComponentLookup<GrenadeData> m_GrenadeDataLookup;
        // The ghost type (grenade) this classification system will process
        int m_GhostType;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_SnapshotDataLookupHelper = new SnapshotDataLookupHelper(ref state,
                SystemAPI.GetSingletonEntity<GhostCollection>(),
                SystemAPI.GetSingletonEntity<SpawnedGhostEntityMap>());
            m_PredictedGhostSpawnLookup = state.GetBufferLookup<PredictedGhostSpawn>();
            m_GrenadeDataLookup = state.GetComponentLookup<GrenadeData>();
            state.RequireForUpdate<GhostSpawnQueue>();
            state.RequireForUpdate<PredictedGhostSpawnList>();
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<GrenadeSpawner>();
            m_GhostType = -1;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (m_GhostType == -1)
            {
                // Lookup the grenade prefab entity in the ghost prefab list, from there we can find the ghost type for this prefab
                var prefabEntity = SystemAPI.GetSingleton<GrenadeSpawner>().Grenade;
                var collectionEntity = SystemAPI.GetSingletonEntity<GhostCollection>();
                var ghostPrefabTypes = state.EntityManager.GetBuffer<GhostCollectionPrefab>(collectionEntity);
                for (int i = 0; i < ghostPrefabTypes.Length; ++i)
                {
                    if (ghostPrefabTypes[i].GhostPrefab == prefabEntity)
                    {
                        m_GhostType = i;
                        break;
                    }
                }
            }
            m_SnapshotDataLookupHelper.Update(ref state);
            m_PredictedGhostSpawnLookup.Update(ref state);
            m_GrenadeDataLookup.Update(ref state);
            var classificationJob = new GrenadeClassificationJob
            {
                snapshotDataLookupHelper = m_SnapshotDataLookupHelper,
                spawnListEntity = SystemAPI.GetSingletonEntity<PredictedGhostSpawnList>(),
                PredictedSpawnListLookup = m_PredictedGhostSpawnLookup,
                grenadeDataLookup = m_GrenadeDataLookup,
                ghostType = m_GhostType
            };
            state.Dependency = classificationJob.Schedule(state.Dependency);
        }

        [WithAll(typeof(GhostSpawnQueue))]
        [BurstCompile]
        partial struct GrenadeClassificationJob : IJobEntity
        {
            public SnapshotDataLookupHelper snapshotDataLookupHelper;
            public Entity spawnListEntity;
            public BufferLookup<PredictedGhostSpawn> PredictedSpawnListLookup;
            public ComponentLookup<GrenadeData> grenadeDataLookup;
            public int ghostType;

            public void Execute(DynamicBuffer<GhostSpawnBuffer> newSpawns, DynamicBuffer<SnapshotDataBuffer> data)
            {
                var predictedSpawnList = PredictedSpawnListLookup[spawnListEntity];
                var snapshotDataLookup = snapshotDataLookupHelper.CreateSnapshotBufferLookup();
                for (int i = 0; i < newSpawns.Length; ++i)
                {
                    var newGhostSpawn = newSpawns[i];
                    if (newGhostSpawn.GhostType != ghostType)
                        continue; // Not a grenade.

                    if (newGhostSpawn.SpawnType != GhostSpawnBuffer.Type.Predicted || newGhostSpawn.PredictedSpawnEntity != Entity.Null)
                        continue;

                    // Mark all the grenade spawns as classified even if not our own predicted spawns
                    // otherwise spawns from other players might be picked up by the default classification system when
                    // it runs when we happen to have a predicted spawn in the predictedSpawnList not yet classified here
                    newGhostSpawn.HasClassifiedPredictedSpawn = true;

                    // Find new ghost spawns (from ghost snapshot) which match the predict spawned ghost type handled by
                    // this classification system. Match the spawn ID data from the new spawn (by lookup it up in
                    // snapshot data) with the spawn IDs of ghosts in the predicted spawn list. When matched we replace
                    // the ghost entity of that new spawn with our predict spawned entity (so the spawn will not result
                    // in a new instantiation).
                    for (int j = 0; j < predictedSpawnList.Length; ++j)
                    {
                        if (newGhostSpawn.GhostType == predictedSpawnList[j].ghostType)
                        {
                            if (snapshotDataLookup.TryGetComponentDataFromSnapshotHistory(newGhostSpawn.GhostType, data, out GrenadeData grenadeData, i))
                            {
                                var spawnIdFromList = grenadeDataLookup[predictedSpawnList[j].entity].SpawnId;
                                if (grenadeData.SpawnId == spawnIdFromList)
                                {
                                    newGhostSpawn.PredictedSpawnEntity = predictedSpawnList[j].entity;
                                    predictedSpawnList[j] = predictedSpawnList[predictedSpawnList.Length - 1];
                                    predictedSpawnList.RemoveAt(predictedSpawnList.Length - 1);
                                    break;
                                }
                            }
                        }
                    }
                    newSpawns[i] = newGhostSpawn;
                }
            }
        }
    }
}
