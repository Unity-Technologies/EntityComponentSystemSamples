using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Hash128 = Unity.Entities.Hash128;

namespace Baking.BlobAssetBakingSystem
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct BlobAssetBakingSystem : ISystem
    {
        NativeParallelHashMap<Hash128, BlobAssetReference<MeshBBBlobAsset>> m_BlobAssetReferences;
        NativeList<Entity> m_EntitiesToProcess;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_BlobAssetReferences =
                new NativeParallelHashMap<Hash128, BlobAssetReference<MeshBBBlobAsset>>(0, Allocator.Persistent);
            m_EntitiesToProcess = new NativeList<Entity>(Allocator.Persistent);

            state.RequireForUpdate<MeshBB>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            m_BlobAssetReferences.Dispose();
            m_EntitiesToProcess.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Get the BlobAssetStore from the BakingSystem
            var blobAssetStore = state.World.GetExistingSystemManaged<BakingSystem>().BlobAssetStore;

            // Collect the BlobAssets that
            // - haven't already been processed in this run
            // - aren't already known to the BlobAssetStore from previous runs (if they are known, save the BlobAssetReference for later)
            foreach (var (rawMesh, entity) in
                     SystemAPI.Query<RefRO<RawMesh>>().WithAll<MeshBB>()
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

            // Create the BlobAssets and BlobAssetReference for each new, unique BlobAsset
            new ComputeBlobDataJob()
            {
                BlobAssetReferences = m_BlobAssetReferences,
                EntitiesToProcess = m_EntitiesToProcess.AsArray(),
                BufferLookup = SystemAPI.GetBufferLookup<MeshVertex>(),
                ComponentLookup = SystemAPI.GetComponentLookup<RawMesh>(),
            }.Schedule(m_EntitiesToProcess.Length, 1).Complete();

            // Assign the BlobAssetReferences to all the entities that have a different BlobAsset than last run
            foreach (var (rawMesh, meshBB) in
                     SystemAPI.Query<RefRO<RawMesh>, RefRW<MeshBB>>())
            {
                var hash = rawMesh.ValueRO.Hash;
                var blobAssetReference = m_BlobAssetReferences[hash];
                blobAssetStore.TryAdd(hash, ref blobAssetReference);
                meshBB.ValueRW.BlobData = blobAssetReference;
            }

            m_BlobAssetReferences.Clear();
            m_EntitiesToProcess.Clear();
        }
    }

    [BurstCompile]
    public struct ComputeBlobDataJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeParallelHashMap<Hash128, BlobAssetReference<MeshBBBlobAsset>> BlobAssetReferences;

        [ReadOnly] public NativeArray<Entity> EntitiesToProcess;
        [ReadOnly] public BufferLookup<MeshVertex> BufferLookup;
        [ReadOnly] public ComponentLookup<RawMesh> ComponentLookup;

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
