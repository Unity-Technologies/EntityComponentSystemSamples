using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Hash128 = Unity.Entities.Hash128;

namespace Advanced.BlobAssets
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct ComputeBlobAssetSystem : ISystem
    {
        NativeParallelHashMap<Hash128, BlobAssetReference<MeshBBBlobAsset>> m_BlobAssetReferences;
        NativeList<Entity> m_EntitiesToProcess;

        public void OnCreate(ref SystemState state)
        {
            m_BlobAssetReferences =
                new NativeParallelHashMap<Hash128, BlobAssetReference<MeshBBBlobAsset>>(0, Allocator.Persistent);
            m_EntitiesToProcess = new NativeList<Entity>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            m_BlobAssetReferences.Dispose();
            m_EntitiesToProcess.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Get the BlobAssetStore from the BakingSystem
            var blobAssetStore = state.World.GetExistingSystemManaged<BakingSystem>().BlobAssetStore;

            // Handles the cleanup of BlobAssets for when BakingSystems are reverted
            HandleCleanup(ref state, blobAssetStore);

            // Collect the BlobAssets that
            // - haven't already been processed in this run
            // - aren't already known to the BlobAssetStore from previous runs (if they are known, save the BlobAssetReference for later)
            foreach (var (rawMesh, entity) in SystemAPI.Query<RefRO<RawMeshComponent>>().WithAll<MeshBBComponent>()
                         .WithEntityAccess())
            {
                if (m_BlobAssetReferences.TryAdd(rawMesh.ValueRO.Hash, BlobAssetReference<MeshBBBlobAsset>.Null))
                {
                    if (blobAssetStore.TryGet<MeshBBBlobAsset>(rawMesh.ValueRO.Hash,
                            out BlobAssetReference<MeshBBBlobAsset> blobAssetReference))
                    {
                        m_BlobAssetReferences[rawMesh.ValueRO.Hash] = blobAssetReference;
                    }
                    else
                    {
                        m_EntitiesToProcess.Add(entity);
                    }
                }
            }

            // Create the BlobAssets and BlobAssetReference for each unique and new BlobAsset
            new ComputeBlobDataJob()
            {
                BlobAssetReferences = m_BlobAssetReferences,
                EntitiesToProcess = m_EntitiesToProcess.AsArray(),
                BufferLookup = SystemAPI.GetBufferLookup<MeshVertex>(),
                ComponentLookup = SystemAPI.GetComponentLookup<RawMeshComponent>(),
            }.ScheduleParallel(m_EntitiesToProcess.Length, 1, default(JobHandle)).Complete();

            // Assign the BlobAssetReferences to all the entities that have a different BlobAsset than last run
            foreach (var (rawMesh, meshBB, cleanup) in
                     SystemAPI.Query<RefRO<RawMeshComponent>, RefRW<MeshBBComponent>, RefRW<CleanupComponent>>())
            {
                var hash = rawMesh.ValueRO.Hash;
                // Add the BlobAssetReference to the BlobAssetStore to handle refCounting if the BlobAsset for this Entity has changed since last run
                if (cleanup.ValueRO.Hash != hash || !meshBB.ValueRO.BlobData.IsCreated)
                {
                    m_BlobAssetReferences.TryGetValue(hash, out BlobAssetReference<MeshBBBlobAsset> blobAssetReference);
                    blobAssetStore.TryAdd(hash, ref blobAssetReference);
                    meshBB.ValueRW.BlobData = blobAssetReference;

                    // Decrease the refCount if the BlobAsset is known and remove it if the refCount becomes zero
                    blobAssetStore.TryRemove<MeshBBBlobAsset>(cleanup.ValueRO.Hash, true);
                    cleanup.ValueRW.Hash = hash;
                }
            }

            m_BlobAssetReferences.Clear();
            m_EntitiesToProcess.Clear();
        }

        public void HandleCleanup(ref SystemState state, BlobAssetStore blobAssetStore)
        {
            // Add the Cleanup Component to the newly created Entities, that do not have it yet
            var addCleanupQuery = SystemAPI.QueryBuilder().WithAll<MeshBBComponent>().WithNone<CleanupComponent>()
                .Build();
            state.EntityManager.AddComponent<CleanupComponent>(addCleanupQuery);


            // Cleanup the BlobAssets and Cleanup Components of newly destroyed Entities
            // Cleanup of the BlobAssets through the BlobAssetStore
            foreach (var cleanup in SystemAPI.Query<RefRO<CleanupComponent>>().WithNone<MeshBBComponent>())
            {
                blobAssetStore.TryRemove<MeshBBBlobAsset>(cleanup.ValueRO.Hash, true);
            }

            // Remove the Cleanup Component from the destroyed Entities
            var removeCleanupQuery = SystemAPI.QueryBuilder().WithAll<CleanupComponent>()
                .WithNone<MeshBBComponent>().Build();
            state.EntityManager.RemoveComponent<CleanupComponent>(removeCleanupQuery);

        }

        [BurstCompile]
        partial struct ComputeBlobDataJob : IJobFor
        {
            [NativeDisableParallelForRestriction]
            public NativeParallelHashMap<Hash128, BlobAssetReference<MeshBBBlobAsset>> BlobAssetReferences;

            [ReadOnly] public NativeArray<Entity> EntitiesToProcess;
            [ReadOnly] public BufferLookup<MeshVertex> BufferLookup;
            [ReadOnly] public ComponentLookup<RawMeshComponent> ComponentLookup;

            public void Execute(int index)
            {
                var rawMesh = ComponentLookup[EntitiesToProcess[index]];
                var buffer = BufferLookup[EntitiesToProcess[index]];

                var minBoundingBox = float3.zero;
                var maxBoundingBox = float3.zero;
                bool hasMesh = buffer.Length > 0;

                if (hasMesh)
                {
                    float xp = float.MinValue, yp = float.MinValue, zp = float.MinValue;
                    float xn = float.MaxValue, yn = float.MaxValue, zn = float.MaxValue;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var p = buffer[i].Value;
                        xp = math.max(p.x, xp);
                        yp = math.max(p.y, yp);
                        zp = math.max(p.z, zp);
                        xn = math.min(p.x, xn);
                        yn = math.min(p.y, yn);
                        zn = math.min(p.z, zn);
                    }

                    minBoundingBox = new float3(xn, yn, zn);
                    maxBoundingBox = new float3(xp, yp, zp);
                }

                using (var builder = new BlobBuilder(Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<MeshBBBlobAsset>();

                    var s = rawMesh.MeshScale;
                    root.MinBoundingBox = minBoundingBox * s;
                    root.MaxBoundingBox = maxBoundingBox * s;

                    BlobAssetReferences[rawMesh.Hash] =
                        builder.CreateBlobAssetReference<MeshBBBlobAsset>(Allocator.Persistent);
                }
            }
        }
    }
}
