using System;
using AOT;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.LowLevel.Unsafe;
using Unity.Transforms;
using UnityEngine.Assertions;

namespace Samples.CustomChunkSerializer
{
    [BurstCompile]
    public struct ChunkSerializer
    {
        //About the snapshot buffer memory layout
        //The snapshot buffer contains entries in this format
        // [tick][ent1 data][ent2 data] ... [entN data]
        // the data has the following layout:
        // uint Tick
        // 4 bytes aligned change mask bits
        // (optional)
        // 4 bytes aligned enable bits state
        // padding (to 16 bytes)
        // component1 (aligned to 16 byte boundary)
        // component2 (aligned to 16 byte boundary)
        // ..
        // componentN (aligned to 16 byte boundary)

        //About the bitsize memory layout.
        //The bitStartAndSize contains the start uint and bit len of the ghost data.
        //Has the following layout
        // [Component1                                ][Component2
        // |ent1 start|ent1 len|..|entN start|entN len||ent1 start|ent1 len|..|entN start|entN len|
        //
        // The stride is NumComponent * (endIndex - startIndex), where endIndex and startIndex are the first and last
        // relevant entity index in the chunk.

        public static PortableFunctionPointer<GhostPrefabCustomSerializer.CollectComponentDelegate> CollectComponentFunc =
                new PortableFunctionPointer<GhostPrefabCustomSerializer.CollectComponentDelegate>(CollectComponents);

        public static PortableFunctionPointer<GhostPrefabCustomSerializer.ChunkSerializerDelegate> SerializerFunc =
            new PortableFunctionPointer<GhostPrefabCustomSerializer.ChunkSerializerDelegate>(SerializeChunk);

        public static PortableFunctionPointer<GhostPrefabCustomSerializer.ChunkPreserializeDelegate> PreSerializerFunc =
            new PortableFunctionPointer<GhostPrefabCustomSerializer.ChunkPreserializeDelegate>(PreSerializeChunk);

        //Custom method to register the component types in a specific order, so that the chunk serializer can be written
        //way more easily.
        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(GhostPrefabCustomSerializer.CollectComponentDelegate))]
        public static void CollectComponents(IntPtr componentTypesPtr, IntPtr componentCountPtr)
        {
            ref var componentTypes = ref GhostComponentSerializer.TypeCast<NativeList<ComponentType>>(componentTypesPtr);
            ref var componentCount = ref GhostComponentSerializer.TypeCast<NativeArray<int>>(componentCountPtr);
            //Root
            componentTypes.Add(ComponentType.ReadWrite<GhostOwner>());
            componentTypes.Add(ComponentType.ReadWrite<LocalTransform>());
            componentTypes.Add(ComponentType.ReadWrite<IntCompo1>());
            componentTypes.Add(ComponentType.ReadWrite<IntCompo2>());
            componentTypes.Add(ComponentType.ReadWrite<IntCompo3>());
            componentTypes.Add(ComponentType.ReadWrite<FloatCompo1>());
            componentTypes.Add(ComponentType.ReadWrite<FloatCompo2>());
            componentTypes.Add(ComponentType.ReadWrite<FloatCompo3>());
            componentTypes.Add(ComponentType.ReadWrite<InterpolatedOnlyComp>());
            componentTypes.Add(ComponentType.ReadWrite<OwnerOnlyComp>());
            componentTypes.Add(ComponentType.ReadWrite<Buf1>());
            componentTypes.Add(ComponentType.ReadWrite<Buf2>());
            componentTypes.Add(ComponentType.ReadWrite<Buf3>());
            componentCount[0] = 13;
            //Child 1
            componentTypes.Add(ComponentType.ReadWrite<IntCompo1>());
            componentTypes.Add(ComponentType.ReadWrite<FloatCompo1>());
            componentTypes.Add(ComponentType.ReadWrite<Buf1>());
            componentCount[1] = 3;
            //Child 2
            componentTypes.Add(ComponentType.ReadWrite<IntCompo2>());
            componentTypes.Add(ComponentType.ReadWrite<FloatCompo2>());
            componentTypes.Add(ComponentType.ReadWrite<Buf2>());
            componentCount[2] = 3;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(GhostPrefabCustomSerializer.CollectComponentDelegate))]
        private static unsafe void PreSerializeChunk(in ArchetypeChunk chunk, in GhostCollectionPrefabSerializer typeData,
            in DynamicBuffer<GhostCollectionComponentIndex> componentIndices,
            ref GhostPrefabCustomSerializer.Context context)
        {
            var indices = (GhostCollectionComponentIndex*)componentIndices.GetUnsafeReadOnlyPtr() + typeData.FirstComponent;
            CopyComponentsToSnapshot(chunk, ref context, typeData, indices);
        }

        const int BaselinesPerEntity = 4;
        //Assumptions made:
        // - components are never removed (so we not check for presence)
        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(GhostPrefabCustomSerializer.ChunkSerializerDelegate))]
        private static unsafe void SerializeChunk(ref ArchetypeChunk chunk,
            in GhostCollectionPrefabSerializer typeData,
            in DynamicBuffer<GhostCollectionComponentIndex> componentIndices,
            ref GhostPrefabCustomSerializer.Context context,
            ref DataStreamWriter tempWriter,
            in StreamCompressionModel compressionModel, ref int lastSerializedEntity)
        {
            var baselinesPerEntity = (IntPtr*)context.baselinePerEntityPtr;
            var sameBaselinePerEntity = (int*)context.sameBaselinePerEntityPtr;
            int* entityBitAndSize = (int*)context.entityStartBit;
            var entityCount = context.endIndex - context.startIndex;
            int* componentBitsSize = entityBitAndSize + 2*entityCount;
            int compBitSizeStride = 2*entityCount;
            var indices = (GhostCollectionComponentIndex*)componentIndices.GetUnsafeReadOnlyPtr() + typeData.FirstComponent;
            if(context.hasPreserializedData == 0)
                CopyComponentsToSnapshot(chunk, ref context, typeData, indices);

            int* dynamicDataSizePerEntity = (int*)context.dynamicDataSizePerEntityPtr;
            for (int ent = context.startIndex; ent < context.endIndex; ++ent)
            {
                var old = tempWriter;
                var entOffset = ent - context.startIndex;
                //Avoid serializing irrelevant entities
                var sameBaselineCount = sameBaselinePerEntity[entOffset];
                if (sameBaselineCount < 0)
                {
                    // This is an irrelevant ghost, do not send . There is no need to reset the
                    //bits size and start bit, because the same check is also done outside.
                    continue;
                }

                //The writer contains data in a different format in the case of the chunk serializer:
                //we are serializing on per "entity" directly here.
                //This give the advantage of being able to early exit without need to serialise other entities if
                //they don't fit (or at least early exiting after the first one that fails)
                //Ideally, (this is a second phase), we can now write directly in the real data stream. Right now
                //this is not possible because we are adding the size of buffers and ghosts data (delta compressed) before
                //the ghost stream.
                var snapshotData = context.snapshotDataPtr + entOffset*context.snapshotStride;
                var changeMaskData = snapshotData + sizeof(int);
                var currentWrittenBits = tempWriter.LengthInBits;
                var baseline0Ptr = baselinesPerEntity[BaselinesPerEntity*entOffset];
                var baseline1Ptr = baselinesPerEntity[BaselinesPerEntity*entOffset + 1];
                var baseline2Ptr = baselinesPerEntity[BaselinesPerEntity*entOffset + 2];
                //This can an IntPtrZero if there are no buffers of the baseline does not exist
                var dynamicDataBaselinePtr = baselinesPerEntity[BaselinesPerEntity*entOffset + 3];
                //This requires to overwrite the snapshot data with all zeros or keep the current predicted baseline (optimal)
                var ghostSendType = GhostSendType.AllClients;
                var sendToOwner = SendToOwnerType.All;
                //the comp bit size already point to the entityBitSize[1] for the first component entry.
                var compBitSize = componentBitsSize + 2*entOffset + 1;
                if (typeData.PredictionOwnerOffset != 0)
                {
                    var isOwner = (context.networkId == *(int*)((byte*)snapshotData + typeData.PredictionOwnerOffset));
                    if (typeData.PartialSendToOwner != 0)
                        sendToOwner = isOwner ? SendToOwnerType.SendToOwner : SendToOwnerType.SendToNonOwner;
                    if (typeData.PartialComponents != 0 && typeData.OwnerPredicted != 0)
                        ghostSendType = isOwner ? GhostSendType.OnlyPredictedClients : GhostSendType.OnlyInterpolatedClients;
                }
                if (baseline2Ptr != IntPtr.Zero)
                {
                    SerializeWithThreeBaselines(snapshotData, context.snapshotOffset, sendToOwner, ghostSendType,
                        indices, ref tempWriter, compressionModel, baseline0Ptr, baseline1Ptr, baseline2Ptr,
                        changeMaskData, context.snapshotDynamicDataPtr, dynamicDataBaselinePtr,
                        ref dynamicDataSizePerEntity[entOffset], compBitSize, compBitSizeStride);
                }
                //Single baseline
                else if (baseline0Ptr != IntPtr.Zero)
                {
                    SerializeWithSingleBaseline(snapshotData, context.snapshotOffset, sendToOwner, ghostSendType,
                        indices, ref tempWriter, compressionModel, baseline0Ptr,
                        changeMaskData, context.snapshotDynamicDataPtr, dynamicDataBaselinePtr,
                        ref dynamicDataSizePerEntity[entOffset], compBitSize, compBitSizeStride);
                }
                //No baseline, we are passing a pointer to a baseline that contains all zero.
                //The advantage of this is that can be extended, to allow using "initial-value"
                //optimization, to send the delta in respect the prefab initial data.
                else
                {
                    SerializeWithSingleBaseline(snapshotData, context.snapshotOffset, sendToOwner, ghostSendType,
                        indices, ref tempWriter, compressionModel, context.zeroBaseline,
                        changeMaskData, context.snapshotDynamicDataPtr, IntPtr.Zero,
                        ref dynamicDataSizePerEntity[entOffset], compBitSize, compBitSizeStride);
                }
                //Count the number of bits for each ghosts.
                entityBitAndSize[2*entOffset] = currentWrittenBits / 32;
                entityBitAndSize[2*entOffset+1] = tempWriter.LengthInBits - currentWrittenBits;
                var missing = 32 - tempWriter.LengthInBits & 31;
                if (missing < 32)
                    tempWriter.WriteRawBits(0, missing);
                if (tempWriter.HasFailedWrites)
                {
                    //If we were able to store at least one entity there is not need to mark the stream
                    //as failed.
                    //Rollback it here and let the outer loop to serialize the full entities.
                    if (entOffset > 0)
                    {
                        tempWriter = old;
                        lastSerializedEntity = ent - 1;
                    }
                    break;
                }
            }
        }

        private static unsafe void CopyComponentsToSnapshot(ArchetypeChunk chunk,
            ref GhostPrefabCustomSerializer.Context context,
            in GhostCollectionPrefabSerializer typeData,
            GhostCollectionComponentIndex* indices)
        {
            var ghostChunkComponentTypesPtr = (DynamicComponentTypeHandle*)context.ghostChunkComponentTypes;

            var maskOffset = 0;
            var snapshotOffset = context.snapshotOffset;
            var dynamicSnapshotOffset = context.dynamicDataOffset;
            var changeMaskUints = GhostComponentSerializer.ChangeMaskArraySizeInUInts(typeData.ChangeMaskBits);
            var snapshotPtr = context.snapshotDataPtr;
            var enableBits = (byte*)(snapshotPtr + sizeof(int) + 4*changeMaskUints);

            //ROOT COMPONENTS
            //GENERATE ONLY THIS
            new Unity_NetCode_Generated_Unity_NetCode.Unity_NetCode_Generated_Unity_NetCode_GhostOwnerGhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr, indices[0], snapshotPtr, ref snapshotOffset);
            new Unity_NetCode_Generated_Unity_Transforms.Unity_NetCode_Generated_Unity_Transforms_TransformDefaultVariantGhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[1], snapshotPtr, ref snapshotOffset);
            CustomGhostSerializerHelpers.CopyEnableBits(chunk, context.startIndex, context.endIndex, context.snapshotStride,
                ref ghostChunkComponentTypesPtr[indices[2].ComponentIndex], enableBits, ref maskOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo1GhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr, indices[2], snapshotPtr, ref snapshotOffset);
            CustomGhostSerializerHelpers.CopyEnableBits(chunk, context.startIndex, context.endIndex, context.snapshotStride,
                ref ghostChunkComponentTypesPtr[indices[3].ComponentIndex], enableBits, ref maskOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo2GhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr, indices[3], snapshotPtr, ref snapshotOffset);
            CustomGhostSerializerHelpers.CopyEnableBits(chunk, context.startIndex, context.endIndex, context.snapshotStride,
                ref ghostChunkComponentTypesPtr[indices[4].ComponentIndex], enableBits, ref maskOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo3GhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[4], snapshotPtr, ref snapshotOffset);
            CustomGhostSerializerHelpers.CopyEnableBits(chunk, context.startIndex, context.endIndex, context.snapshotStride,
                ref ghostChunkComponentTypesPtr[indices[5].ComponentIndex], enableBits, ref maskOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo1GhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[5], snapshotPtr, ref snapshotOffset);
            CustomGhostSerializerHelpers.CopyEnableBits(chunk, context.startIndex, context.endIndex, context.snapshotStride,
                ref ghostChunkComponentTypesPtr[indices[6].ComponentIndex], enableBits, ref maskOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo2GhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[6], snapshotPtr, ref snapshotOffset);
            CustomGhostSerializerHelpers.CopyEnableBits(chunk, context.startIndex, context.endIndex, context.snapshotStride,
                ref ghostChunkComponentTypesPtr[indices[7].ComponentIndex], enableBits, ref maskOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo3GhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[7], snapshotPtr, ref snapshotOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_InterpolatedOnlyCompGhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[8], snapshotPtr, ref snapshotOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_OwnerOnlyCompGhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[9], snapshotPtr, ref snapshotOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf1GhostComponentSerializer().CopyBufferToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[10], snapshotPtr, ref snapshotOffset, ref dynamicSnapshotOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf2GhostComponentSerializer().CopyBufferToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[11], snapshotPtr, ref snapshotOffset, ref dynamicSnapshotOffset);
            new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf3GhostComponentSerializer().CopyBufferToSnapshot(chunk, ref context,
                ghostChunkComponentTypesPtr,indices[12], snapshotPtr, ref snapshotOffset, ref dynamicSnapshotOffset);

            //CHILD COMPONENTS
            var linkedGroup = chunk.GetBufferAccessor(ref context.linkedEntityGroupTypeHandle);
            for (int ent = context.startIndex; ent < context.endIndex; ++ent)
            {
                var childEnableMaskOffset = maskOffset;
                var childSnapshotOffset = snapshotOffset;

                //GENERATE ONLY THIS
                var childEnt = linkedGroup[ent][1].Value;
                var childEntityStorageInfo = context.childEntityLookup[childEnt];
                CustomGhostSerializerHelpers.CopyEnableBits(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, childEntityStorageInfo.IndexInChunk+1, context.snapshotStride,
                    ref ghostChunkComponentTypesPtr[indices[13].ComponentIndex], enableBits, ref childEnableMaskOffset);
                new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo1GhostComponentSerializer().CopyChildComponentToSnapshot(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, ref context,
                    ghostChunkComponentTypesPtr,indices[13], snapshotPtr, ref childSnapshotOffset);
                CustomGhostSerializerHelpers.CopyEnableBits(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, childEntityStorageInfo.IndexInChunk+1, context.snapshotStride,
                    ref ghostChunkComponentTypesPtr[indices[14].ComponentIndex], enableBits, ref childEnableMaskOffset);
                new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo1GhostComponentSerializer().CopyChildComponentToSnapshot(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, ref context,
                    ghostChunkComponentTypesPtr,indices[14], snapshotPtr, ref childSnapshotOffset);
                new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf1GhostComponentSerializer().CopyChildBufferToSnapshot(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, ref context,
                    ghostChunkComponentTypesPtr,indices[15], snapshotPtr, ref childSnapshotOffset, ref dynamicSnapshotOffset);

                childEnt = linkedGroup[ent][2].Value;
                childEntityStorageInfo = context.childEntityLookup[childEnt];
                CustomGhostSerializerHelpers.CopyEnableBits(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, childEntityStorageInfo.IndexInChunk+1, context.snapshotStride,
                    ref ghostChunkComponentTypesPtr[indices[16].ComponentIndex], enableBits, ref childEnableMaskOffset);
                new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo2GhostComponentSerializer().CopyChildComponentToSnapshot(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, ref context,
                    ghostChunkComponentTypesPtr,indices[16], snapshotPtr, ref childSnapshotOffset);
                CustomGhostSerializerHelpers.CopyEnableBits(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, childEntityStorageInfo.IndexInChunk+1, context.snapshotStride,
                    ref ghostChunkComponentTypesPtr[indices[17].ComponentIndex], enableBits, ref childEnableMaskOffset);
                new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo2GhostComponentSerializer().CopyChildComponentToSnapshot(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, ref context,
                    ghostChunkComponentTypesPtr,indices[17], snapshotPtr, ref childSnapshotOffset);
                new CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf2GhostComponentSerializer().CopyChildBufferToSnapshot(childEntityStorageInfo.Chunk, childEntityStorageInfo.IndexInChunk, ref context,
                    ghostChunkComponentTypesPtr,indices[18], snapshotPtr, ref childSnapshotOffset, ref dynamicSnapshotOffset);

                Assert.IsTrue(childEnableMaskOffset <= typeData.EnableableBits);

                snapshotPtr += context.snapshotStride;
                enableBits += context.snapshotStride;
            }
        }

        private static unsafe void SerializeWithSingleBaseline(IntPtr snapshotData, int snapshotOffset,
            SendToOwnerType sendToOwnerMask, GhostSendType sendTypeMask,
            GhostCollectionComponentIndex* indices,
            ref DataStreamWriter writer, in StreamCompressionModel compressionModel, IntPtr baseline0Ptr,
            IntPtr changeMaskData,
            IntPtr dynamicSnapshotData, IntPtr baselineDynamicData, ref int dynamicDataSizePerEntity,
            int* compBitSize, int compBitSizeStride)
        {
            var changeMaskOffset = 0;
            //GENERATE ONLY THIS
            compBitSize[0*compBitSizeStride] = default(Unity_NetCode_Generated_Unity_NetCode.Unity_NetCode_Generated_Unity_NetCode_GhostOwnerGhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[1*compBitSizeStride] = default(Unity_NetCode_Generated_Unity_Transforms.Unity_NetCode_Generated_Unity_Transforms_TransformDefaultVariantGhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[2*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo1GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[3*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo2GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[4*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo3GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[5*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo1GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[6*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo2GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[7*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo3GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[8*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_InterpolatedOnlyCompGhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel, (int)(indices[8].SendMask & sendTypeMask) | (int)(indices[8].SendToOwner & sendToOwnerMask));
            compBitSize[9*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_OwnerOnlyCompGhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel, (int)(indices[9].SendMask & sendTypeMask) | (int)(indices[9].SendToOwner & sendToOwnerMask));
            compBitSize[10*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf1GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
            compBitSize[11*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf2GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
            compBitSize[12*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf3GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
            compBitSize[13*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo1GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[14*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo1GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[15*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf1GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
            compBitSize[16*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo2GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[17*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo2GhostComponentSerializer).SerializeComponentSingleBaseline(snapshotData,baseline0Ptr,changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref writer, compressionModel);
            compBitSize[18*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf2GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
        }

        private static unsafe void SerializeWithThreeBaselines(IntPtr snapshotData, int snapshotOffset,
            SendToOwnerType sendToOwnerMask, GhostSendType sendTypeMask,
            GhostCollectionComponentIndex* indices,
            ref DataStreamWriter writer, in StreamCompressionModel compressionModel,
            IntPtr baseline0Ptr, IntPtr baseline1Ptr, IntPtr baseline2Ptr, IntPtr changeMaskData,
            IntPtr dynamicSnapshotData, IntPtr baselineDynamicData, ref int dynamicDataSizePerEntity,
            int* compBitSize, int compBitSizeStride)
        {
            var predictor = new GhostDeltaPredictor(
                new NetworkTick { SerializedData = GhostComponentSerializer.TypeCast<uint>(snapshotData) },
                new NetworkTick { SerializedData = GhostComponentSerializer.TypeCast<uint>(baseline0Ptr) },
                new NetworkTick { SerializedData = GhostComponentSerializer.TypeCast<uint>(baseline1Ptr) },
                new NetworkTick { SerializedData = GhostComponentSerializer.TypeCast<uint>(baseline2Ptr) });
            var changeMaskOffset = 0;

            //GENERATE ONLY THIS
            compBitSize[0*compBitSizeStride] = default(Unity_NetCode_Generated_Unity_NetCode.Unity_NetCode_Generated_Unity_NetCode_GhostOwnerGhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[1*compBitSizeStride] = default(Unity_NetCode_Generated_Unity_Transforms.Unity_NetCode_Generated_Unity_Transforms_TransformDefaultVariantGhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[2*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo1GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[3*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo2GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[4*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo3GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[5*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo1GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[6*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo2GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[7*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo3GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[8*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_InterpolatedOnlyCompGhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel, (int)(indices[8].SendMask & sendTypeMask) | (int)(indices[8].SendToOwner & sendToOwnerMask));
            compBitSize[9*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_OwnerOnlyCompGhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel, (int)(indices[9].SendMask & sendTypeMask) | (int)(indices[9].SendToOwner & sendToOwnerMask));
            compBitSize[10*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf1GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
            compBitSize[11*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf2GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
            compBitSize[12*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf3GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
            compBitSize[13*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo1GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[14*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo1GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[15*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf1GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity,ref writer, compressionModel);
            compBitSize[16*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_IntCompo2GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[17*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_FloatCompo2GhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,baseline1Ptr, baseline2Ptr, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);
            compBitSize[18*compBitSizeStride] = default(CustomSerializer_Generated_Samples_CustomChunkSerializer.CustomSerializer_Generated_Samples_CustomChunkSerializer_Buf2GhostComponentSerializer).SerializeBuffer(snapshotData,baseline0Ptr, dynamicSnapshotData, baselineDynamicData, changeMaskData,ref changeMaskOffset, ref snapshotOffset, ref dynamicDataSizePerEntity, ref writer, compressionModel);
        }
    }
}
