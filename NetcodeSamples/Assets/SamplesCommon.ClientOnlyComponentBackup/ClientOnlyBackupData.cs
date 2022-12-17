using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport.Utilities;

namespace Unity.NetCode.Samples
{
    /// <summary>
    /// Component added to all ghosts to mark they have been pre-processed.
    /// </summary>
    public struct ClientOnlyProcessed : IComponentData
    {
    }

    /// <summary>
    /// Component added to all ghosts when there are client-only data to backup inside the prediction loop.
    /// </summary>
    internal struct ClientOnlyBackup : ICleanupComponentData, IDisposable
    {
        //Contains the raw copy of the component/buffer data present on the client
        //DATA LAYOUT
        // [tick (32bit)][enablebits (aligned 4 byte)] ... padding .. | [compdata]
        // compdata start at at 16byte aligned boundary (for sake of simd access)
        public UnsafeList<byte> ComponentBackup;
        /// <summary>
        /// The next backup slot we are writing into.
        /// </summary>
        private short m_backupWrPtr;
        /// <summary>
        /// The next backup slot we are reading from.
        /// </summary>
        private short m_backupRdPtr;
        /// <summary>
        /// The number of backup slot available in the backup buffer
        /// </summary>
        private short m_backupCapacity;
        /// <summary>
        /// The number of backup slot available in the backup buffer
        /// </summary>
        private short m_backupSlotUsed;

        /// <summary>
        /// Create and inialize the client-only backup component buffer with the specified capacity.
        /// </summary>
        /// <param name="slotSize"></param>
        /// <param name="capacity"></param>
        public ClientOnlyBackup(int slotSize, int capacity=32)
        {
            ComponentBackup = new UnsafeList<byte>(capacity*slotSize, Allocator.Persistent);
            for (int i=0;i<ComponentBackup.Length;++i)
            {
                ComponentBackup.ElementAt(i) = 0x1f;
            }
            m_backupCapacity = (short)capacity;
            m_backupSlotUsed = 0;
            m_backupWrPtr = 0;
            m_backupRdPtr = 0;
        }

        /// <summary>
        /// Relese the component backup resources
        /// </summary>
        /// <returns></returns>
        public void Dispose()
        {
            ComponentBackup.Dispose();
        }

        /// <summary>
        /// Return the number of uint necessary to store the client-only component enabled bits.
        /// </summary>
        /// <param name="numComponents"></param>
        /// <returns></returns>
        private static int EnableBitIntSize(int numComponents)
        {
            return (numComponents + 31) / 32;
        }
        /// <summary>
        /// The size in bytes of the reserved space for
        /// </summary>
        /// <param name="numComponents"></param>
        /// <returns></returns>
        internal static int EnableBitByteSize(int numComponents)
        {
            return sizeof(int) * EnableBitIntSize(numComponents);
        }


        public bool IsEmpty => m_backupSlotUsed == 0;
        public bool IsFull => m_backupSlotUsed == m_backupCapacity;
        public int UsedSlot => m_backupSlotUsed;
        public int Capacity => m_backupCapacity;

        public void Clear()
        {
            m_backupSlotUsed = 0;
            m_backupRdPtr = 0;
            m_backupWrPtr = 0;
        }

        public void Resize(int newCapacity, int slotSize)
        {
            if(newCapacity == m_backupCapacity)
                return;

            var oldBufferLength = ComponentBackup.Length;
            ComponentBackup.Resize(newCapacity*slotSize);
            m_backupCapacity = (short)newCapacity;
            //Move around the wrapped around portion to the newest allocated area such that the m_backupWrPtr > m_backupRdPtr
            //   | w w w w w | r r r r r | n n n n n n n n n |
            // becomes
            //   | - - - - -  | r r r r r | w w w w n n n n n |
            //                                      ^ --- new write position
            // This just minimize the number of memory moves required
            // Ideally we don't want to have another memcpy when we resize. But that is unfortunately sort of unavoidable, since
            // we don't have control of the UnsafeList buffer reallocation.
            if (m_backupWrPtr <= m_backupRdPtr)
            {
                //move data from 0 up to the backupWr pointer in front
                if (m_backupWrPtr > 0)
                {
                    unsafe
                    {
                        var source = ComponentBackup.Ptr;
                        var dest = ComponentBackup.Ptr + oldBufferLength;
                        UnsafeUtility.MemMove(dest, source, m_backupWrPtr*slotSize);
                    }
                }
                //always move the m_backupWrPtr in front
                m_backupWrPtr = (short)(m_backupRdPtr + m_backupSlotUsed);
            }
        }

        public void GrowBufferIfFull(int slotSize)
        {
            if (m_backupSlotUsed == m_backupCapacity)
            {
                //Grow twice as large
                Resize(m_backupCapacity * 2, slotSize);
            }
        }

        //Return a slot that can be used to write the backup. Internally advance the ring-buffer head the new position
        public int AcquireBackupSlot()
        {
            Assertions.Assert.IsTrue(m_backupSlotUsed < m_backupCapacity);
            var current = m_backupWrPtr;
            //Advance the backup pointer to the next position
            m_backupWrPtr = (short)((m_backupWrPtr + 1) % m_backupCapacity);
            ++m_backupSlotUsed;
            return current;
        }

        //Consume all backup slots that has a tick less or equal than the target tick. Reduce the consumed buffer slots and
        //advance the read position.
        public void RemoveBackupsOlderThan(NetworkTick targetTick, int backupSize)
        {
            if (m_backupSlotUsed == 0)
                return;

            unsafe
            {
                var ptr = ComponentBackup.Ptr + m_backupRdPtr*backupSize;
                var tick = default(NetworkTick);
                while (m_backupSlotUsed > 0)
                {
                    tick.SerializedData = *(uint*)ptr;
                    if (!tick.IsValid || tick.IsNewerThan(targetTick))
                        break;
                    *(uint*)ptr = 0;
                    --m_backupSlotUsed;
                    ++m_backupRdPtr;
                    if (m_backupRdPtr >= m_backupCapacity)
                    {
                        m_backupRdPtr = 0;
                        ptr = ComponentBackup.Ptr;
                    }
                    else
                    {
                        ptr += backupSize;
                    }
                }
            }
        }

        // return the slot for the predictionStartTick or -1 if not found
        public readonly int GetSlotForTick(NetworkTick predictionStartTick, int backupSize)
        {
            Assertions.Assert.IsTrue(m_backupSlotUsed > 0);
            unsafe
            {
                var oldestBackupPtr = ComponentBackup.Ptr + m_backupRdPtr*backupSize;
                var tick = new NetworkTick{SerializedData = *(uint*)oldestBackupPtr};
                //If the tick are equals use this slot
                if (tick == predictionStartTick)
                    return m_backupRdPtr;

                //It the oldest tick we have is newer, just use this one as best approximation. No older tick are present in the buffer anyway
                if (tick.IsNewerThan(predictionStartTick))
                    return m_backupRdPtr;

                //In normal case scenario the client is ahead of the server.
                //The PredictionStartTick should be usually less the current simulated tick. As such it should be in the buffer.
                //However, if the client is lagging a little behind (ex: initial in game connection or is trying to caching up)
                //it may be possible that the latest simulated tick we did is less the latest snapshot received by the server.
                //If for any reason the PredictionStartTick is larger than the latest backup tick we have, restoring from the backup
                //is not making sense, since the best approximation we have (in term of prediction) is the current state of the components
                var lastWrittenSlot = (m_backupWrPtr - 1) % m_backupCapacity;
                var latestBackupPtr = ComponentBackup.Ptr + lastWrittenSlot*backupSize;
                var latestTick = new NetworkTick{SerializedData = *(uint*)latestBackupPtr};
                if (predictionStartTick.IsNewerThan(latestTick))
                    return -1;

                //Calculate the delta and move the backup point the desired slot.
                int delta = predictionStartTick.TicksSince(tick);
                var slotIndex = (m_backupRdPtr + delta) % m_backupCapacity;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                tick.SerializedData = *(uint*)(ComponentBackup.Ptr + slotIndex*backupSize);
                //Tick should be the same
                Assertions.Assert.IsTrue(tick==predictionStartTick);
#endif
                return slotIndex;
            }
        }
    }

    /// <summary>
    /// Store information about the component type (size, and type) and the index to retrieve the
    /// <see cref="Unity.Entities.DynamicComponentTypeHandle"/> from the <see cref="ClientOnlyCollection"/>.
    /// </summary>
    internal struct ClientOnlyBackupInfo
    {
        public ComponentType ComponentType;
        //The size of the component inside the backup. If the component type is a buffer, it is the single element size.
        public int ComponentSize;
        //index inside the the client-only components collection. It is used to retrieve the corresponing
        //dynamic component type handle.
        public int ComponentIndex;
        //the entity index in the hierarchy. 0 is the root.
        public int EntityIndex;
    }

    /// <summary>
    /// The ClientOnlyBackupMetadata struct is associated to each prefab that contains client-only data, and it used to retrieve
    /// the ClientOnlyBackupInfo list, for a given ghost type, inside the clientOnlyBackupInfo collection.
    /// </summary>
    internal struct ClientOnlyBackupMetadata
    {
        //[start, end) range in the ClientOnlyBackupInfo collection.
        public int componentBegin;
        public int componentEnd;
        //num component in the root entity
        public int numRootComponents;
        //The size of the component backup (fixed). It include the tick, enabled bits and all component data. See the ClientOnlyBackup data layour
        public int backupSize;
    }
}
