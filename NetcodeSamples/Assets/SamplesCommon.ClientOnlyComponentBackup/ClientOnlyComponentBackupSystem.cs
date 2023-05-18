using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.LowLevel.Unsafe;

namespace Unity.NetCode.Samples
{
    /// <summary>
    /// System responsible for:
    /// - creating the ClientOnlyCollection and the necessary metadata for processing ghosts with client-only components.
    /// - add to all ghosts (that present or not the client-only components) a state component, the ClientOnlyBackup.
    /// - backup the client-only components data for every full tick and store them inside the ClientOnlyBackup buffer.
    /// <para>
    /// System used to make a backup of all client-only component state. The system run at the end of the prediction loop,
    /// and store inside the <see cref="ClientOnlyBackup"/> history buffer the state of components for each full predicted tick.
    /// Partial ticks are not saved.
    /// </para>
    /// <para>
    /// The backup consist of a mem-copy of the components data and, if the some of the component also implements
    /// the <see cref="IEnableableComponent"/>, their enable bits.
    /// </para>
    /// <remarks>
    /// The size of the <see cref="ClientOnlyBackup"/> buffer is not fixed and can grow,to accomodate both latency and
    /// ghosts update frequency. The size of the buffer is still bounded, due to fact the oldest backup are removed and
    /// the slot reused.
    /// </remarks>
    /// <para>
    /// The saved components states are then used to restore the the components data when a new snapshot is received from the server.
    /// See <see cref="ClientOnlyComponentRestoreSystem"/> for more information.
    /// </para>
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(GhostPredictionHistorySystem))]
    public partial struct ClientOnlyComponentBackupSystem : ISystem
    {
        private const int InitialBackupCapacity = 16;
        private EntityQuery m_predictedGhostsWithClientOnlyBackup;
        private EntityQuery m_predictedGhostsNotProcessed;
        private EntityQuery m_predictedPrespawendGhostsNotProcessed;
        private EntityQuery m_destroyedGhostsWithClientOnlyBackup;

        private EntityStorageInfoLookup m_childEntityLookup;
        private BufferTypeHandle<LinkedEntityGroup> m_linkedEntityGroupHandle;
        private ComponentTypeHandle<ClientOnlyBackup> m_backupTypeHandle;
        private ComponentTypeHandle<GhostType> m_ghostTypeHandle;

        private NativeList<ComponentType> m_clientOnlyComponentTypes;
        private NativeList<ClientOnlyBackupInfo> m_clientOnlyBackupInfoCollection;
        private NativeHashMap<GhostType, ClientOnlyBackupMetadata> m_ghostTypeToPrefabMetadata;
        private ClientOnlyTypeHandleList m_clientOnlyTypeHandleList;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
            queryBuilder.WithAll<Simulate>();
            queryBuilder.WithAll<PredictedGhost>();
            queryBuilder.WithAll<ClientOnlyBackup>();
            m_predictedGhostsWithClientOnlyBackup = state.GetEntityQuery(queryBuilder);
            queryBuilder.Reset();
            queryBuilder.WithAll<PredictedGhost>();
            queryBuilder.WithAll<GhostType>();
            queryBuilder.WithNone<ClientOnlyProcessed>();
            queryBuilder.WithNone<PreSpawnedGhostIndex>();
            m_predictedGhostsNotProcessed = state.GetEntityQuery(queryBuilder);
            queryBuilder.Reset();
            queryBuilder.WithAll<PredictedGhost>();
            queryBuilder.WithAll<GhostType>();
            queryBuilder.WithNone<ClientOnlyProcessed>();
            queryBuilder.WithAll<PreSpawnedGhostIndex>();
            m_predictedPrespawendGhostsNotProcessed = state.GetEntityQuery(queryBuilder);
            queryBuilder.Reset();
            queryBuilder.WithAll<ClientOnlyBackup>();
            queryBuilder.WithNone<PredictedGhost>();
            m_destroyedGhostsWithClientOnlyBackup = state.GetEntityQuery(queryBuilder);

            m_clientOnlyBackupInfoCollection = new NativeList<ClientOnlyBackupInfo>(Allocator.Persistent);
            m_clientOnlyComponentTypes = new NativeList<ComponentType>(32, Allocator.Persistent);
            m_ghostTypeToPrefabMetadata = new NativeHashMap<GhostType, ClientOnlyBackupMetadata>(128, Allocator.Persistent);
            m_clientOnlyTypeHandleList = default(ClientOnlyTypeHandleList);

            m_backupTypeHandle = state.GetComponentTypeHandle<ClientOnlyBackup>();
            m_ghostTypeHandle = state.GetComponentTypeHandle<GhostType>(true);
            m_childEntityLookup = state.GetEntityStorageInfoLookup();
            m_linkedEntityGroupHandle = state.GetBufferTypeHandle<LinkedEntityGroup>(true);

            //Create the singleton.
            var types = new NativeArray<ComponentType>(1, Allocator.Temp);
            types[0] = ComponentType.ReadWrite<ClientOnlyCollection>();
            var singleton = state.EntityManager.CreateEntity(state.EntityManager.CreateArchetype(types));
            state.EntityManager.SetComponentData(singleton, new ClientOnlyCollection
            {
                ProcessedPrefabs = 0,
                ClientOnlyComponentTypes = m_clientOnlyComponentTypes,
                BackupInfoCollection = m_clientOnlyBackupInfoCollection,
                GhostTypeToPrefabMetadata = m_ghostTypeToPrefabMetadata
            });

            state.RequireForUpdate<GhostCollection>();
            state.RequireForUpdate<EnableClientOnlyBackup>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            m_clientOnlyComponentTypes.Dispose();
            m_clientOnlyBackupInfoCollection.Dispose();
            m_ghostTypeToPrefabMetadata.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var clientOnlyCollection = ref SystemAPI.GetSingletonRW<ClientOnlyCollection>().ValueRW;
            if(clientOnlyCollection.ClientOnlyComponentTypes.Length == 0)
                return;
            var ghostCollection = SystemAPI.GetSingleton<GhostCollection>();
            if(!ghostCollection.IsInGame || ghostCollection.NumLoadedPrefabs == 0)
                return;

            if (clientOnlyCollection.ProcessedPrefabs < ghostCollection.NumLoadedPrefabs)
            {
                var ghostPrefabs = SystemAPI.GetSingletonBuffer<GhostCollectionPrefab>(true);
                for (int i = clientOnlyCollection.ProcessedPrefabs; i < ghostCollection.NumLoadedPrefabs; ++i)
                {
                    clientOnlyCollection.ProcessGhostTypePrefab(ghostPrefabs[i].GhostType, ghostPrefabs[i].GhostPrefab, state.EntityManager);
                    ++clientOnlyCollection.ProcessedPrefabs;
                }
            }
            if(clientOnlyCollection.ProcessedPrefabs == 0)
                return;

            if (!m_destroyedGhostsWithClientOnlyBackup.IsEmpty)
            {
                var job = new DisposeBackupJob();
                job.Run(m_destroyedGhostsWithClientOnlyBackup);
                state.EntityManager.RemoveComponent<ClientOnlyBackup>(m_destroyedGhostsWithClientOnlyBackup);
            }

            //Add to ghost the necessary state to backup the components and buffers.
            if (!m_predictedGhostsNotProcessed.IsEmpty)
            {
                var entities = m_predictedGhostsNotProcessed.ToEntityArray(Allocator.Temp);
                var ghostTypes = m_predictedGhostsNotProcessed.ToComponentDataArray<GhostType>(Allocator.Temp);
                //Mark entities as processed
                state.EntityManager.AddComponent<ClientOnlyProcessed>(m_predictedGhostsNotProcessed);
                for(int ent=0;ent<entities.Length;++ent)
                {
                    var ghostType = ghostTypes[ent];
                    //If the type does not have any client only component, skip.
                    if(!clientOnlyCollection.GhostTypeToPrefabMetadata.ContainsKey(ghostType))
                        continue;
                    //Adding component one by one like that is very very slow. Ideally I would like to add the component "per chunk"
                    //but it is not really possible (or is not that easy to achieve)
                    var clientOnlyMetadata = clientOnlyCollection.GhostTypeToPrefabMetadata[ghostType];
                    var clientOnlyBackup = new ClientOnlyBackup(slotSize:clientOnlyMetadata.backupSize, capacity:InitialBackupCapacity);
                    state.EntityManager.AddComponentData(entities[ent], clientOnlyBackup);
                }
            }

            //Pre-spawned ghosts has a special path, because the handling is a little different. In particular we can just assign
            //the processed flag to the query (the fastest way) but we need to inspect entities one by one.
            //That generate quite a lot of burden in term of structural changes.
            if (!m_predictedPrespawendGhostsNotProcessed.IsEmpty)
            {
                var entities = m_predictedPrespawendGhostsNotProcessed.ToEntityArray(Allocator.Temp);
                var ghostTypes = m_predictedPrespawendGhostsNotProcessed.ToComponentDataArray<GhostType>(Allocator.Temp);
                state.EntityManager.AddComponent<ClientOnlyProcessed>(m_predictedPrespawendGhostsNotProcessed);
                for(int ent=0;ent<entities.Length;++ent)
                {
                    var ghostType = ghostTypes[ent];
                    if (!clientOnlyCollection.GhostTypeToPrefabMetadata.ContainsKey(ghostType))
                        continue;
                    var clientOnlyMetadata = clientOnlyCollection.GhostTypeToPrefabMetadata[ghostType];
                    var clientOnlyBackup = new ClientOnlyBackup(slotSize:clientOnlyMetadata.backupSize, capacity:InitialBackupCapacity);
                    state.EntityManager.AddComponentData(entities[ent], clientOnlyBackup);
                }
            }

            //Only backup full server tick. Partial tick will always start from the last full simulated tick so no need to backup
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (networkTime.IsPartialTick)
                return;
            m_childEntityLookup.Update(ref state);
            m_linkedEntityGroupHandle.Update(ref state);
            m_backupTypeHandle.Update(ref state);
            m_ghostTypeHandle.Update(ref state);
            m_clientOnlyTypeHandleList.CreateOrUpdateTypeHandleList(ref state, clientOnlyCollection.ClientOnlyComponentTypes.AsArray());
            var backupJob = new BackupJob
            {
                childEntityLookup = m_childEntityLookup,
                ghostTypeHandle = m_ghostTypeHandle,
                linkedEntityGroupHandle = m_linkedEntityGroupHandle,
                componentTypeHandles = m_clientOnlyTypeHandleList,
                clientOnlyComponentCollection = clientOnlyCollection.BackupInfoCollection,
                prefabMetadata = clientOnlyCollection.GhostTypeToPrefabMetadata,
                backupTypeHandle = m_backupTypeHandle,
                serverTick = networkTime.ServerTick,
                netDebug = SystemAPI.GetSingleton<NetDebug>()
            };
            state.Dependency = backupJob.ScheduleParallel(m_predictedGhostsWithClientOnlyBackup, state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClientOnlyBackup))]
        [WithNone(typeof(PredictedGhost))]
        partial struct DisposeBackupJob : IJobEntity
        {
            public void Execute(ref ClientOnlyBackup backup)
            {
                backup.Dispose();
            }
        }

        //Little class that help writing component backup. Abstract some pointer manipulation and add some
        //boundary checks (in the editor)
        unsafe ref struct ClientOnlyBackupWriter
        {
            private readonly int* backupEnableBitPtr;
            private uint* backupTick;
            private byte* backupCompDataPtr;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private byte* beginCompDataPtr;
            private int backupSize;
#endif

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckBounds()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if((backupCompDataPtr - beginCompDataPtr) >= backupSize)
                    throw new ArgumentOutOfRangeException();
#endif
            }

            public ClientOnlyBackupWriter(byte* bufPtr, int compDataOffset, int slotSize)
            {
                backupTick = (uint*)bufPtr;
                backupEnableBitPtr = (int*)(bufPtr + sizeof(uint));
                backupCompDataPtr = (bufPtr + compDataOffset);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                beginCompDataPtr = backupCompDataPtr;
                backupSize = slotSize;
#endif
            }

            //Skip the component/buffer backup. Reset the data to 0 and advance the backup pointers
            public void Skip(in ClientOnlyBackupInfo comp)
            {
                CheckBounds();
                UnsafeUtility.MemClear(backupCompDataPtr, comp.ComponentSize);
                backupCompDataPtr += comp.ComponentSize;
            }

            public void WriteTick(NetworkTick serverTick)
            {
                *backupTick = serverTick.SerializedData;
            }

            //Store the enable bit for the component inside the backup. The backup slot data format is
            //  4 bytes       4 bytes            4 bytes
            // [   tick   ][EnableBits 0][EnableBits 1]
            //              3130 ...   0  64 ...    32
            // the bits are stored left to right
            public void BackupEnableBitForComponent(int compIdx, int ent, [ReadOnly] long* bitArrayPtr)
            {
                int entIdx = 1 << (ent & 0x3f);
                long compEnableFroEntity = ((bitArrayPtr[ent >> 6] >> entIdx) & 0x1);
                int bitIdx = 1 << (compIdx & 0x1f);
                backupEnableBitPtr[compIdx >> 5] &= ~bitIdx;
                backupEnableBitPtr[compIdx >> 5] |= (int)(bitIdx * compEnableFroEntity);
            }

            //Store the component data into the backup buffer.
            public void BackupComponent([ReadOnly] byte* compDataPtr, in ClientOnlyBackupInfo comp)
            {
                CheckBounds();
                //TODO: probably better to optimise that for small component data size (like 8/64 bytes)
                UnsafeUtility.MemCpy(backupCompDataPtr, compDataPtr, comp.ComponentSize);
                backupCompDataPtr += comp.ComponentSize;
            }
        }

        [BurstCompile]
        unsafe struct BackupJob : IJobChunk
        {
            [ReadOnly] public EntityStorageInfoLookup childEntityLookup;
            [ReadOnly] public ComponentTypeHandle<GhostType> ghostTypeHandle;
            public ComponentTypeHandle<ClientOnlyBackup> backupTypeHandle;
            [ReadOnly] public BufferTypeHandle<LinkedEntityGroup> linkedEntityGroupHandle;

            [ReadOnly] public ClientOnlyTypeHandleList componentTypeHandles;
            [ReadOnly] public NativeList<ClientOnlyBackupInfo> clientOnlyComponentCollection;
            [ReadOnly] public NativeHashMap<GhostType, ClientOnlyBackupMetadata> prefabMetadata;
            public NetworkTick serverTick;
            public NetDebug netDebug;
            public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                //Lookup for the ghost type. If the chunk contains predicted prespawed-ghost and the type has not been assigned
                //yet, we skip the backup.
                var ghostTypes = chunk.GetNativeArray(ref ghostTypeHandle);
                var firstGhostType = ghostTypes[0];
                //Retrieve the backup metadata. This should have been constructed already by the system before scheduling the jobs
                if (!prefabMetadata.TryGetValue(firstGhostType, out var metadata))
                {
                    netDebug.LogError($"Unable to find client-only backup metadata for ghost with ghost type {firstGhostType}");
                    return;
                }
                //Prepare thw writers and copy the ghost data in the backup buffer.
                //Components and buffers data are backup on per entity basis, with some slightly different code in between root and child entites.
                var backupWriters = stackalloc ClientOnlyBackupWriter[chunk.Count];
                InitBackupWriters(chunk, metadata, backupWriters);
                BackupComponents(chunk, metadata, backupWriters);
            }

            private void BackupComponents(in ArchetypeChunk chunk, in ClientOnlyBackupMetadata metadata, ClientOnlyBackupWriter *backupWriters)
            {
                int chunkEntityCount = chunk.Count;
                //store the server tick for the current slot
                for (int ent = 0; ent < chunkEntityCount; ++ent)
                    backupWriters[ent].WriteTick(serverTick);

                //Backup all the root component for the whole chunk.
                var iterEnd = metadata.componentBegin + metadata.numRootComponents;
                var compIdx = metadata.componentBegin;
                //just a convenient way to add some boundary checks
                var typeHandles = new UnsafeList<DynamicComponentTypeHandle>(componentTypeHandles.Ptr, clientOnlyComponentCollection.Length);
                for (;compIdx < iterEnd; ++compIdx)
                {
                    var comp = clientOnlyComponentCollection[compIdx];
                    //Buffers aren't supported
                    Unity.Assertions.Assert.IsFalse(comp.ComponentType.IsBuffer);
                    var typeHandle = typeHandles[comp.ComponentIndex];
                    if (!chunk.Has(ref typeHandle))
                    {
                        for (int ent = 0; ent < chunkEntityCount; ++ent)
                            backupWriters[ent].Skip(comp);
                        continue;
                    }
                    if (comp.ComponentType.IsEnableable)
                    {
                        var handle = typeHandle;
                        var bitArray = chunk.GetEnableableBits(ref handle);
                        var bitArrayPtr = (long*)UnsafeUtility.AddressOf(ref bitArray);
                        for (int ent = 0; ent < chunkEntityCount; ++ent)
                        {
                            backupWriters[ent].BackupEnableBitForComponent(compIdx, ent, bitArrayPtr);
                        }
                    }
                    var compDataPtr = (byte*)chunk
                        .GetDynamicComponentDataArrayReinterpret<byte>(ref typeHandle, comp.ComponentSize)
                        .GetUnsafeReadOnlyPtr();
                    for (int ent = 0; ent < chunkEntityCount; ++ent)
                    {
                        backupWriters[ent].BackupComponent(compDataPtr, comp);
                        compDataPtr += comp.ComponentSize;
                    }
                }
                //backup all child entities components.
                if (!chunk.Has(ref linkedEntityGroupHandle))
                    return;
                var entityGroup = chunk.GetBufferAccessor(ref linkedEntityGroupHandle);
                for (int childCompIdx = compIdx; childCompIdx < metadata.componentEnd; ++childCompIdx)
                {
                    var comp = clientOnlyComponentCollection[childCompIdx];
                    var typeHandle = typeHandles[comp.ComponentIndex];
                    for (int ent = 0; ent < chunkEntityCount; ++ent)
                    {
                        if (entityGroup[ent].Length <= comp.EntityIndex)
                        {
                            backupWriters[ent].Skip(comp);
                            continue;
                        }
                        var childEnt = entityGroup[ent][comp.EntityIndex].Value;
                        var childChunk = childEntityLookup[childEnt].Chunk;
                        var indexInChunk = childEntityLookup[childEnt].IndexInChunk;
                        if (!childChunk.Has(ref typeHandle))
                        {
                            backupWriters[ent].Skip(comp);
                            continue;
                        }
                        if (comp.ComponentType.IsEnableable)
                        {
                            var handle = typeHandle;
                            var bitArray = childChunk.GetEnableableBits(ref handle);
                            var bitArrayPtr = (long*)UnsafeUtility.AddressOf(ref bitArray);
                            backupWriters[ent].BackupEnableBitForComponent(childCompIdx, indexInChunk, bitArrayPtr);
                        }
                        var compDataPtr = (byte*)childChunk
                            .GetDynamicComponentDataArrayReinterpret<byte>(ref typeHandle, comp.ComponentSize)
                            .GetUnsafeReadOnlyPtr();
                        compDataPtr += comp.ComponentSize * indexInChunk;
                        backupWriters[ent].BackupComponent(compDataPtr, comp);
                    }
                }
            }

            private void InitBackupWriters(ArchetypeChunk chunk, in ClientOnlyBackupMetadata metadata, ClientOnlyBackupWriter* backupWriters)
            {
                var enableBitsIntSize = ClientOnlyBackup.EnableBitByteSize(metadata.componentEnd - metadata.componentBegin);
                var compDataStartOffset = GhostComponentSerializer.SnapshotSizeAligned(sizeof(uint) + enableBitsIntSize);
                //use the raw pointer for accessing the component data by ref. This is used to acquire and grow the buffer.
                var states = (ClientOnlyBackup*)chunk.GetNativeArray(ref backupTypeHandle).GetUnsafePtr();
                for (int ent = 0, chunkEntityCount = chunk.Count; ent < chunkEntityCount; ++ent)
                {
                    states[ent].GrowBufferIfFull(metadata.backupSize);
                    var backupSlotIndex = states[ent].AcquireBackupSlot();
                    var slotOffset = metadata.backupSize * backupSlotIndex;
                    byte* backupDataPtr = states[ent].ComponentBackup.Ptr + slotOffset;
                    backupWriters[ent] = new ClientOnlyBackupWriter(backupDataPtr, compDataStartOffset, metadata.backupSize);
                }
            }
        }
    }
}
