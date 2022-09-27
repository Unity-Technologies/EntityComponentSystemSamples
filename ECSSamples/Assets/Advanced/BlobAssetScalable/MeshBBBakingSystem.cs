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

public class MeshBBBaker : Baker<MeshToBoundingBoxAuthoring>
{
    public override void Bake(MeshToBoundingBoxAuthoring authoring)
    {
        // new
        var blobFactoryPoints = new NativeList<float3>(Allocator.TempJob);
        var vertices = new List<Vector3>(4096);
        Profiler.BeginSample("Bake_BuildHashAndPush");

        // Compute the blob asset hash based on Authoring properties
        var mesh = authoring.Mesh;
        var hasMesh = mesh != null;
        var meshHashCode = hasMesh ? mesh.GetHashCode() : 0;
        var hash = new Hash128((uint) meshHashCode, (uint) authoring.MeshScale.GetHashCode(), 0, 0);

        // Query the context to determine if we need to build the BlobAsset
        if (!TryGetBlobAssetReference(hash, out BlobAssetReference<MeshBBBlobAsset> blobAssetReference))
        {
            Profiler.BeginSample("CopyVertices");
            float xp = float.MinValue, yp = float.MinValue, zp = float.MinValue;
            float xn = float.MaxValue, yn = float.MaxValue, zn = float.MaxValue;

            // Copy the mesh vertices into the point array
            if (hasMesh)
            {
                mesh.GetVertices(vertices);
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
            var vertexCount = hasMesh ? mesh.vertexCount : 0;
            var setting = new MeshBBFactorySettings
            {
                Hash = hash, MeshScale = authoring.MeshScale, PointStartIndex = 0,
                PointCount = vertexCount, MinBoundingBox = new float3(xn, yn, zn),
                MaxBoundingBox = new float3(xp, yp, zp)
            };

            Profiler.EndSample();
            Profiler.BeginSample("Bake_CreateBlobAssets");

            // Step two, compute BlobAssets
            var job = new ComputeMeshBBAssetJob(new NativeArray<MeshBBFactorySettings>(1, Allocator.TempJob){[0] = setting}, blobFactoryPoints.AsArray());
            job.Schedule(1,1).Complete();
            blobAssetReference = job.BlobAssets[0];
            AddBlobAssetWithCustomHash(ref blobAssetReference, setting.Hash);
        }

        Profiler.EndSample();
        Profiler.BeginSample("Bake_CreateECS");

        // Create the ECS component for the given GameObject
        var entity = GetEntity(authoring);
        AddComponent(entity, new MeshBBComponent(blobAssetReference));
#if !ENABLE_TRANSFORM_V1
        AddComponent(entity, new LocalToWorldTransform {Value = UniformScaleTransform.FromPosition(authoring.transform.position)});
#else
        AddComponent(entity, new Translation {Value = authoring.transform.position});
#endif
        Profiler.EndSample();
        blobFactoryPoints.Dispose();
    }
}

