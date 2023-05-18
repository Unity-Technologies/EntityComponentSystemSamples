using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.LowLevel.Unsafe;

namespace Unity.NetCode.Samples
{
    /// <summary>
    /// Singleton that contains all the registered client-only component types and prefab metadata.
    /// <para>
    /// The client-only backup systems only backup the state of the component that are registered to the
    /// <see cref="ClientOnlyCollection"/>. The components <b>must</b> be registered before 'going' in game.
    /// </para>
    /// </summary>
    public struct ClientOnlyCollection : IComponentData
    {
        internal NativeList<ComponentType> ClientOnlyComponentTypes;
        internal NativeList<ClientOnlyBackupInfo> BackupInfoCollection;
        internal NativeHashMap<GhostType, ClientOnlyBackupMetadata> GhostTypeToPrefabMetadata;
        internal int ProcessedPrefabs;
        /// <summary>
        /// Flag used to check if a component can be registered. Once the first ghost prefabs has been processed,
        /// it is not possible to add other component to the collection.
        /// </summary>
        internal bool CanRegisterComponents => ProcessedPrefabs == 0;

        /// <summary>
        /// Call this method to register the component as client-only and make it part of the backup.
        /// The registration must be done before the game start (connection goes in game).
        /// A good practice is to create a system that is create <see cref="CreateAfterAttribute"/> after the
        /// <see cref="ClientOnlyComponentBackupSystem"/> (so tha can access the singleton) and register the component once.
        /// </summary>
        /// <param name="componentType"></param>
        public void RegisterClientOnlyComponentType(in ComponentType componentType)
        {
            if (!CanRegisterComponents)
                throw new InvalidOperationException("cannot register client-only component after prefabs has been processed or the connection is in game");
            if (ClientOnlyComponentTypes.IndexOf(componentType) >= 0)
                return;
            ClientOnlyComponentTypes.Add(componentType);
        }

        internal void ProcessGhostTypePrefab(GhostType ghostType, Entity entity, EntityManager entityManager)
        {
            int first = BackupInfoCollection.Length;
            int componentBackupSize = 0;
            int numRootComponents = 0;
            AddComponentForEntity(entityManager, entity, 0, ref componentBackupSize);
            numRootComponents = BackupInfoCollection.Length - first;
            if (entityManager.HasBuffer<LinkedEntityGroup>(entity))
            {
                var leg = entityManager.GetBuffer<LinkedEntityGroup>(entity);
                for (int i = 1; i < leg.Length; i++)
                    AddComponentForEntity(entityManager, leg[i].Value, i, ref componentBackupSize);
            }
            if (BackupInfoCollection.Length != first)
            {
                //add tick and enable bitmask array to the backup size. Buffer size are re-calculated dynamically based on the buffer
                //contents by the job
                var enableBitsSize = ClientOnlyBackup.EnableBitByteSize(BackupInfoCollection.Length - first);
                var compDataStartOffset = GhostComponentSerializer.SnapshotSizeAligned(sizeof(int) + enableBitsSize);
                componentBackupSize = GhostComponentSerializer.SnapshotSizeAligned(componentBackupSize + compDataStartOffset);
                GhostTypeToPrefabMetadata.Add(ghostType, new ClientOnlyBackupMetadata
                {
                    componentBegin = first,
                    componentEnd = BackupInfoCollection.Length,
                    numRootComponents = numRootComponents,
                    backupSize = componentBackupSize,
                });
            }
        }

        private void AddComponentForEntity(EntityManager entityManager, Entity entity, int entityIndex, ref int backupSize)
        {
            using var componentTypes = entityManager.GetComponentTypes(entity);
            foreach (var componentType in componentTypes)
            {
                int index = ClientOnlyComponentTypes.IndexOf(componentType);
                if(index < 0)
                    continue;

                //This introduce a little bit redundancy but at least do not requires two memory fetches to get this data
                var info = new ClientOnlyBackupInfo
                {
                    ComponentType = componentType,
                    ComponentSize = componentType.IsBuffer
                        ? TypeManager.GetTypeInfo(componentType.TypeIndex).ElementSize
                        : TypeManager.GetTypeInfo(componentType.TypeIndex).TypeSize,
                    ComponentIndex = index,
                    EntityIndex = entityIndex
                };
                BackupInfoCollection.Add(info);
                Unity.Assertions.Assert.IsFalse(componentType.IsBuffer);
                backupSize += TypeManager.GetTypeInfo(componentType.TypeIndex).TypeSize;
            }
        }
    }
}
