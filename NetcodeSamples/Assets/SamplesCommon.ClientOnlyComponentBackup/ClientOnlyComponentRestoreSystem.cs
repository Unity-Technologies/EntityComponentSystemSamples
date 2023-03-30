using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode.LowLevel.Unsafe;

namespace Unity.NetCode.Samples
{
    /// <summary>
    /// System used to restore the state of the client-only component present on predicted ghost when a
    /// new snapshot from server is received.
    /// <para>
    /// The system must run after the <see cref="GhostUpdateSystem"/> (responsible
    /// to update the state of all ghosts) and before the <see cref="PredictedSimulationSystemGroup"/>
    /// </para> to guarantee the predicted ghosts components states are all synced to the same last received tick.
    /// <para>
    /// The restoring process copy the component data and enable bits from the <see cref="ClientOnlyBackup"/> for
    /// all ghosts that received a new snapshot.
    /// If the server for which we want to restore the data is not found in the backup buffer, the components data are
    /// left unchanged.
    /// </para>
    /// <para>
    /// After the component data has been restore for the last received tick, all the client-only buffers
    /// are shrank, by removing the oldest backup. In particular:
    /// <para>- For the ghosts that has received the new snapshot, the buffer is cleared</para>
    /// <para>- For all ghosts that were not present in the snapshot, all backup with a tick older than the last received tick are removed.</para>
    /// </para>
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [UpdateAfter(typeof(GhostUpdateSystem))]
    public partial struct ClientOnlyComponentRestoreSystem : ISystem
    {
        private EntityQuery predictedGhostsWithClientOnlyBackup;
        private EntityStorageInfoLookup childEntityLookup;
        private BufferTypeHandle<LinkedEntityGroup> linkedEntityGroupHandle;
        private ComponentTypeHandle<ClientOnlyBackup> backupTypeHandle;
        private ComponentTypeHandle<GhostType> ghostTypeHandle;
        private ComponentTypeHandle<PredictedGhost> predictedGhostTypeHandle;
        //Internal to make it accessible by the tests
        internal ClientOnlyTypeHandleList componentTypeHandles;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
            queryBuilder.WithAll<PredictedGhost>();
            queryBuilder.WithAll<GhostType>();
            queryBuilder.WithAll<ClientOnlyBackup>();
            predictedGhostsWithClientOnlyBackup = state.GetEntityQuery(queryBuilder);

            linkedEntityGroupHandle = state.GetBufferTypeHandle<LinkedEntityGroup>(true);
            childEntityLookup = state.GetEntityStorageInfoLookup();
            backupTypeHandle = state.GetComponentTypeHandle<ClientOnlyBackup>();
            ghostTypeHandle = state.GetComponentTypeHandle<GhostType>(true);
            predictedGhostTypeHandle = state.GetComponentTypeHandle<PredictedGhost>(true);

            state.RequireForUpdate<EnableClientOnlyBackup>();
            state.RequireForUpdate<ClientOnlyCollection>();
            state.RequireForUpdate<NetworkSnapshotAck>();
            state.RequireForUpdate(predictedGhostsWithClientOnlyBackup);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var clientOnlyCollection = SystemAPI.GetSingleton<ClientOnlyCollection>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            // complete the dependency in order to access the NetworkSnapshotAck (because written in NetworkStreamReceiveSystem)
            state.CompleteDependency();
            var ackComponent = SystemAPI.GetSingleton<NetworkSnapshotAck>();

            backupTypeHandle.Update(ref state);
            ghostTypeHandle.Update(ref state);
            predictedGhostTypeHandle.Update(ref state);
            childEntityLookup.Update(ref state);
            linkedEntityGroupHandle.Update(ref state);
            componentTypeHandles.CreateOrUpdateTypeHandleList(ref state, clientOnlyCollection.ClientOnlyComponentTypes.AsArray());

            //copy the component data from backup buffer and clear the client only backup buffers based on
            //on the last received snapshot tick.
            var job = new RestoreFromBackup
            {
                ghostTypeHandle = ghostTypeHandle,
                predictedGhostComponentTypeHandle = predictedGhostTypeHandle,
                linkedEntityGroupHandle = linkedEntityGroupHandle,
                childEntityLookup = childEntityLookup,
                backupTypeHandle = backupTypeHandle,
                componentTypeHandles = componentTypeHandles,
                clientOnlyComponentCollection = clientOnlyCollection.BackupInfoCollection.AsArray().AsReadOnly(),
                prefabMetadata = clientOnlyCollection.GhostTypeToPrefabMetadata.AsReadOnly(),
                serverTick = networkTime.ServerTick,
                lastReceivedSnapshotByLocal = ackComponent.LastReceivedSnapshotByLocal,
                netDebug = SystemAPI.GetSingleton<NetDebug>()
            };
            state.Dependency = job.ScheduleParallel(predictedGhostsWithClientOnlyBackup, state.Dependency);
        }

        [BurstCompile]
        unsafe struct RestoreFromBackup : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<GhostType> ghostTypeHandle;
            [ReadOnly] public ComponentTypeHandle<PredictedGhost> predictedGhostComponentTypeHandle;
            [ReadOnly] public BufferTypeHandle<LinkedEntityGroup> linkedEntityGroupHandle;
            public ComponentTypeHandle<ClientOnlyBackup> backupTypeHandle;
            public EntityStorageInfoLookup childEntityLookup;
            public ClientOnlyTypeHandleList componentTypeHandles;
            public NativeArray<ClientOnlyBackupInfo>.ReadOnly clientOnlyComponentCollection;
            public NativeHashMap<GhostType, ClientOnlyBackupMetadata>.ReadOnly prefabMetadata;
            public NetworkTick serverTick;
            //The latest received tick from the server. All backup history before that tick can be cleared
            public NetworkTick lastReceivedSnapshotByLocal;
            public NetDebug netDebug;

            public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var ghostComponents = chunk.GetNativeArray(ref ghostTypeHandle);
                var ghostType = ghostComponents[0];
                //Early exit if type is not setup or the ghost type is not found yet in the backup info.
                if (!prefabMetadata.TryGetValue(ghostType, out var metadata))
                {
                    netDebug.LogError($"Unable to find client-only backup metadata for ghost with ghost type {ghostType}");
                    return;
                }
                var predictedGhosts = chunk.GetNativeArray(ref predictedGhostComponentTypeHandle);
                //Use unsafe pointer to avoid the local copy and to let access the backup data by ref
                var backups = (ClientOnlyBackup*)chunk.GetNativeArray(ref backupTypeHandle).GetUnsafeReadOnlyPtr();
                for (int ent = 0; ent < chunk.Count; ++ent)
                {
                    //This is the state we need to restore from. Can be:
                    // - the last full prediction tick (in case the prediction is continuing)
                    // - the last received tick (in case of new data)
                    // - the current tick (so nothing do)
                    var predictionStartTick = predictedGhosts[ent].PredictionStartTick;
                    //If the backup didn't run yet or the entity is just spawned, use the current state.
                    //IF the start tick is the the current target tick, there is nothing to backup.
                    if (backups[ent].IsEmpty || !predictionStartTick.IsValid || predictionStartTick == serverTick)
                        continue;
                    var backupSlotIndex = backups[ent].GetSlotForTick(predictionStartTick, metadata.backupSize);
                    //if there is no backup available do nothing and use the current component state
                    if (backupSlotIndex < 0)
                        continue;
                    var bufferReader = new BackupReader(backupSlotIndex, backups[ent], metadata);
                    RestoreComponentsFromBackup(chunk, ref bufferReader, ent, metadata);
                    //Reset the length of the buffer to 0. We are going to re-predict all the ticks from the the prediction start
                    //to for this entity
                    if(predictionStartTick == lastReceivedSnapshotByLocal)
                        backups[ent].Clear();
                }
                //Remove all backup with tick less or equal than the last received tick from the server.
                //Ghosts are not not going to rollback to this tick anymore.
                for (int ent = 0, chunkEntityCount = chunk.Count; ent < chunkEntityCount; ++ent)
                    backups[ent].RemoveBackupsOlderThan(lastReceivedSnapshotByLocal, metadata.backupSize);
            }

            private void RestoreComponentsFromBackup(ArchetypeChunk chunk, ref BackupReader reader, int ent,
                in ClientOnlyBackupMetadata metadata)
            {
                //We have a valid backup slot to use. Restore components and buffers.
                var iterEnd = metadata.componentBegin + metadata.numRootComponents;
                var compIdx = metadata.componentBegin;
                var typeHandles = new UnsafeList<DynamicComponentTypeHandle>(componentTypeHandles.Ptr, clientOnlyComponentCollection.Length);
                for (;compIdx < iterEnd; ++compIdx)
                {
                    var comp = clientOnlyComponentCollection[compIdx];
                    var typeHandle = typeHandles[comp.ComponentIndex];
                    Assertions.Assert.IsFalse(comp.ComponentType.IsBuffer);
                    if (!chunk.Has(ref typeHandle))
                    {
                        reader.Skip(comp);
                        continue;
                    }
                    if (comp.ComponentType.IsEnableable)
                    {
                        var handle = typeHandle;
                        chunk.SetComponentEnabled(ref handle, ent, reader.IsEnabled(compIdx));
                    }
                    var compDataPtr = (byte*)chunk
                        .GetDynamicComponentDataArrayReinterpret<byte>(ref typeHandle, comp.ComponentSize)
                        .GetUnsafePtr();
                    compDataPtr += comp.ComponentSize * ent;
                    reader.RestoreComponent(comp, compDataPtr);
                }
                //If the chunk does not have a linked entity group we can't restore any child component.
                if (!chunk.Has(ref linkedEntityGroupHandle))
                    return;

                var entityGroup = chunk.GetBufferAccessor(ref linkedEntityGroupHandle);
                for (var childCompIdx = compIdx; childCompIdx < metadata.componentEnd; ++childCompIdx)
                {
                    var comp = clientOnlyComponentCollection[childCompIdx];
                    Assertions.Assert.IsFalse(comp.ComponentType.IsBuffer);
                    var typeHandle = typeHandles[comp.ComponentIndex];
                    //NOTE: this a safety condition in case the entity group is changed and some child removed.
                    //However, is a necessary but not sufficient condition: it is always possible to
                    //change the entity or removing and add entities and the length will be same.
                    //This does not provide a strong guarantee about which entity we are suppose to expect here,
                    //neither is archetype.
                    if (entityGroup[ent].Length <= comp.EntityIndex)
                    {
                        reader.Skip(comp);
                        continue;
                    }
                    var childEnt = entityGroup[ent][comp.EntityIndex].Value;
                    var childChunk = childEntityLookup[childEnt].Chunk;
                    var indexInChunk = childEntityLookup[childEnt].IndexInChunk;
                    if (!childChunk.Has(ref typeHandle))
                    {
                        reader.Skip(comp);
                        continue;
                    }
                    if (comp.ComponentType.IsEnableable)
                    {
                        var handle = typeHandle;
                        childChunk.SetComponentEnabled(ref handle, ent, reader.IsEnabled(childCompIdx));
                    }
                    var compDataPtr = (byte*)childChunk
                        .GetDynamicComponentDataArrayReinterpret<byte>(ref typeHandle, comp.ComponentSize)
                        .GetUnsafeReadOnlyPtr();
                    compDataPtr += comp.ComponentSize * indexInChunk;
                    reader.RestoreComponent(comp, compDataPtr);
                }
            }
            //Little class that help reading backup from a buffer and that check boundary conditions (in the editor)
            struct BackupReader
            {
                private readonly uint* enableBits;
                private byte* compBackupPtr;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                readonly byte* beginCompDataPtr;
                readonly int backupSize;
#endif

                public BackupReader(int backupSlotIndex, in ClientOnlyBackup backup, in ClientOnlyBackupMetadata metadata)
                {
                    var compDataSlotPtr = backup.ComponentBackup.Ptr + backupSlotIndex * metadata.backupSize;
                    var compDataOffset = GhostComponentSerializer.SnapshotSizeAligned(sizeof(uint) + ClientOnlyBackup.EnableBitByteSize(metadata.componentEnd - metadata.componentBegin));
                    enableBits = (uint*)(compDataSlotPtr + sizeof(uint));
                    compBackupPtr = compDataSlotPtr + compDataOffset;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    beginCompDataPtr = compBackupPtr;
                    backupSize = metadata.backupSize;
#endif
                }

                [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
                private void CheckBounds()
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if((compBackupPtr - beginCompDataPtr) >= backupSize)
                        throw new ArgumentOutOfRangeException();
#endif
                }

                public void Skip(in ClientOnlyBackupInfo comp)
                {
                    compBackupPtr += comp.ComponentSize;
                    CheckBounds();
                }
                public bool IsEnabled(int compIdx)
                {
                    int bitIdx = 1 << (compIdx & 0x1f);
                    return (enableBits[compIdx >> 5] & bitIdx) != 0;
                }
                public void RestoreComponent(in ClientOnlyBackupInfo comp, byte *compDataPtr)
                {
                    CheckBounds();
                    UnsafeUtility.MemCpy(compDataPtr, compBackupPtr, comp.ComponentSize);
                    compBackupPtr += comp.ComponentSize;
                }
            }
        }
    }
}
