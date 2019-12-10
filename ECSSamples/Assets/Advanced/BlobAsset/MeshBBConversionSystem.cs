using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using Hash128 = Unity.Entities.Hash128;

public struct MeshBBFactorySettings
{
    public Hash128 Hash;
    public float MeshScale;
    public int PointStartIndex;
    public int PointCount;
    public float3 MinBoundingBox;
    public float3 MaxBoundingBox;
}

public class MeshBBConversionSystem : GameObjectConversionSystem
{
    protected override void OnCreate()
    {
        base.OnCreate();
        GetEntityQuery(ComponentType.ReadOnly<MeshToBoundingBoxsAuthoring>());
    }

    protected override void OnUpdate()
    {
        var blobFactoryPoints = new NativeList<float3>(Allocator.TempJob);
        int curPointIndex = 0;
        var vertices = new List<Vector3>(4096);
        var processBlobAssets = new NativeList<Hash128>(32, Allocator.Temp);
    
        Profiler.BeginSample("Conv_BuildHashAndPush");

        using (var context = new BlobAssetComputationContext<MeshBBFactorySettings, MeshBBBlobAsset>(BlobAssetStore, 128, Allocator.Temp))
        {
            // First step: for all changed GameObjects we compute the hash of their blob asset then get the asset or register its computation
            Entities.ForEach((MeshToBoundingBoxsAuthoring auth) =>
            {
                // Compute the blob asset hash based on Authoring properties
                var hasMesh = auth.Mesh != null;
                var meshHashCode = hasMesh ? auth.Mesh.GetHashCode() : 0;
                var hash = new Hash128((uint)meshHashCode, (uint)auth.MeshScale.GetHashCode(), 0, 0);

                // Query the context to determine if we need to build the BlobAsset
                processBlobAssets.Add(hash);
                context.AssociateBlobAssetWithGameObject(hash, auth.gameObject);
                if (context.NeedToComputeBlobAsset(hash))
                {
                    Profiler.BeginSample("CopyVertices");

                    float xp = float.MinValue, yp = float.MinValue, zp = float.MinValue;
                    float xn = float.MaxValue, yn = float.MaxValue, zn = float.MaxValue;
                    
                    // Copy the mesh vertices into the point array
                    if (hasMesh)
                    {
                        auth.Mesh.GetVertices(vertices);
                        for (int i = 0; i < vertices.Count; i++)
                        {
                            var p = vertices[i];
                            xp = math.max(p.x, xp);
                            yp = math.max(p.y, yp);
                            zp = math.max(p.z, zp);
                            xn = math.min(p.x, xn);
                            yn = math.min(p.y, yn);
                            zn = math.min(p.z, zn);
                            blobFactoryPoints.Add(new float3(p.x, p.y, p.z));
                        }
                    }
                    else
                    {
                        xp = yp = zp = xn = yn = zn = 0;
                    }

                    Profiler.EndSample();

                    // Record this blob asset for computation
                    var vertexCount = hasMesh ? auth.Mesh.vertexCount : 0;
                    var setting = new MeshBBFactorySettings { Hash = hash, MeshScale = auth.MeshScale, PointStartIndex = curPointIndex, PointCount = vertexCount, MinBoundingBox = new float3(xn, yn, zn), MaxBoundingBox = new float3(xp, yp, zp)};
                    curPointIndex += vertexCount;

                    context.AddBlobAssetToCompute(hash, setting);
                }
            });

            Profiler.EndSample();

            Profiler.BeginSample("Conv_CreateBlobAssets");

            using (var settings = context.GetSettings(Allocator.TempJob))
            {
                // Step two, compute BlobAssets
                var job = new ComputeMeshBBAssetJob(settings, blobFactoryPoints.AsArray());
                job.Schedule(job.Settings.Length, 1).Complete();

                for (int i = 0; i < settings.Length; i++)
                {
                    context.AddComputedBlobAsset(settings[i].Hash, job.BlobAssets[i]);
                }
                job.BlobAssets.Dispose();
            }

            Profiler.EndSample();

            Profiler.BeginSample("Conv_CreateECS");

            // Third step, create the ECS component with the associated blob asset
            var index = 0;
            Entities.ForEach((MeshToBoundingBoxsAuthoring auth) =>
            {
                context.GetBlobAsset(processBlobAssets[index++], out var blob);

                // Create the ECS component for the given GameObject
                var entity = GetPrimaryEntity(auth);

                DstEntityManager.AddComponentData(entity, new MeshBBComponent(blob));
            });

            Profiler.EndSample();
            blobFactoryPoints.Dispose();
            processBlobAssets.Dispose();
        }
    }
}

[BurstCompile]
public struct ComputeMeshBBAssetJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<MeshBBFactorySettings> Settings;
    [ReadOnly] public NativeArray<float3> Vertices;

    public NativeArray<BlobAssetReference<MeshBBBlobAsset>> BlobAssets;

    public ComputeMeshBBAssetJob(NativeArray<MeshBBFactorySettings> settings, NativeArray<float3> vertices)
    {
        Settings = settings;
        Vertices = vertices;
        BlobAssets = new NativeArray<BlobAssetReference<MeshBBBlobAsset>>(settings.Length, Allocator.TempJob);
    }

    public void Execute(int index)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        var settings = Settings[index];
        ref var root = ref builder.ConstructRoot<MeshBBBlobAsset>();
        var array = builder.Allocate(ref root.Vertices, settings.PointCount);

        var s = settings.MeshScale;
        root.MeshScale = s;
        root.MinBoundingBox = settings.MinBoundingBox * s;
        root.MaxBoundingBox = settings.MaxBoundingBox * s;
        
        for (int i = 0; i < array.Length; i++)
        {
            var v1 = Vertices[settings.PointStartIndex + i];
            array[i] = new float3(v1.x * s, v1.y * s, v1.z * s);
        }
        
        BlobAssets[index] = builder.CreateBlobAssetReference<MeshBBBlobAsset>(Allocator.Persistent);
        builder.Dispose();
    }
}
