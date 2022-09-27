using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Entities.Graphics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class TestEntityCreationAPI : MonoBehaviour
{
    public int EntityCount = 10000;
    public float ObjectScale = 0.1f;
    public float Radius = 10;
    public float Twists = 16;
    public List<Mesh> Meshes;
    public Material Material;

    [GenerateTestsForBurstCompatibility]
    public struct SpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public int EntityCount;
        public int MeshCount;
        public float ObjectScale;
        public float Radius;
        public float Twists;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [ReadOnly]
        public NativeArray<RenderBounds> MeshBounds;

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);
            // Prototype has all correct components up front, can use SetComponent
            Ecb.SetComponent(index, e, new LocalToWorld {Value = ComputeTransform(index)});
            Ecb.SetComponent(index, e, new MaterialColor() {Value = ComputeColor(index)});
            // MeshBounds must be set according to the actual mesh for culling to work.
            int meshIndex = index % MeshCount;
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, meshIndex));
            Ecb.SetComponent(index, e, MeshBounds[meshIndex]);
        }

        public float4 ComputeColor(int index)
        {
            float t = (float) index / (EntityCount - 1);
            var color = Color.HSVToRGB(t, 1, 1);
            return new float4(color.r, color.g, color.b, 1);
        }

        public float4x4 ComputeTransform(int index)
        {
            float t = (float) index / (EntityCount - 1);

            float h = 2 * Radius;
            float r = math.sin(t * math.PI) * Radius;

            float phi = t * Twists * (2 * math.PI);

            float x = math.cos(phi) * r;
            float z = math.sin(phi) * r;
            float y = t * h - Radius;

            float4x4 M = float4x4.TRS(
                new float3(x, y, z),
                quaternion.identity,
                new float3(ObjectScale));

            return M;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        EntityCommandBuffer ecbJob = new EntityCommandBuffer(Allocator.TempJob);

        var filterSettings = RenderFilterSettings.Default;
        filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
        filterSettings.ReceiveShadows = false;

        var renderMeshArray = new RenderMeshArray(new[] {Material}, Meshes.ToArray());
        var renderMeshDescription = new RenderMeshDescription
        {
            FilterSettings = filterSettings,
            LightProbeUsage = LightProbeUsage.Off,
        };

        var prototype = entityManager.CreateEntity();
        RenderMeshUtility.AddComponents(
            prototype,
            entityManager,
            renderMeshDescription,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        entityManager.AddComponentData(prototype, new MaterialColor());

        var bounds = new NativeArray<RenderBounds>(Meshes.Count, Allocator.TempJob);
        for (int i = 0; i < bounds.Length; ++i)
            bounds[i] = new RenderBounds {Value = Meshes[i].bounds.ToAABB()};

        // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
        // which can be either a Prefab or an entity created at run time like in this sample.
        // This is the fastest and most efficient way to create entities at run time.
        var spawnJob = new SpawnJob
        {
            Prototype = prototype,
            Ecb = ecbJob.AsParallelWriter(),
            EntityCount = EntityCount,
            MeshCount = Meshes.Count,
            MeshBounds = bounds,
            ObjectScale = ObjectScale,
            Radius = Radius,
            Twists = Twists,
        };

        var spawnHandle = spawnJob.Schedule(EntityCount, 128);
        bounds.Dispose(spawnHandle);

        spawnHandle.Complete();

        ecbJob.Playback(entityManager);
        ecbJob.Dispose();
        entityManager.DestroyEntity(prototype);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
