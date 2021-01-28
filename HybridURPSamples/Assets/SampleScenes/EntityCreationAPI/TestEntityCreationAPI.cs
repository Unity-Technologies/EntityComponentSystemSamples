using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class TestEntityCreationAPI : MonoBehaviour
{
    public int EntityCount = 10000;
    public int MainThreadEntityCount = 500;
    public float ObjectScale = 0.1f;
    public float Radius = 10;
    public float Twists = 16;
    public Mesh Mesh;
    public Material Material;

    [BurstCompatible]
    public struct SpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public int EntityCount;
        public float ObjectScale;
        public float Radius;
        public float Twists;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);
            // Prototype has all correct components up front, can use SetComponent
            Ecb.SetComponent(index, e, new LocalToWorld {Value = ComputeTransform(index)});
            Ecb.SetComponent(index, e, new MaterialColor() {Value = ComputeColor(index)});
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
        EntityCommandBuffer ecbMainThread = new EntityCommandBuffer(Allocator.Temp);

        var desc = new RenderMeshDescription(
            Mesh,
            Material,
            shadowCastingMode: ShadowCastingMode.Off,
            receiveShadows: false);

        var prototype = entityManager.CreateEntity();
        RenderMeshUtility.AddComponents(
            prototype,
            entityManager,
            desc);
        entityManager.AddComponentData(prototype, new MaterialColor());

        // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
        // which can be either a Prefab or an entity created at run time like in this sample.
        // This is the fastest and most efficient way to create entities at run time.
        var spawnJob = new SpawnJob
        {
            Prototype = prototype,
            Ecb = ecbJob.AsParallelWriter(),
            EntityCount = EntityCount,
            ObjectScale = ObjectScale,
            Radius = Radius,
            Twists = Twists,
        };

        int numJobEntities = EntityCount - MainThreadEntityCount;
        var spawnHandle = spawnJob.Schedule(numJobEntities, 128);

        // Spawn a small portion in the main thread to test that the ECB API works.
        // This is NOT the recommended way, this simply tests that this API works.
        for (int i = 0; i < MainThreadEntityCount; ++i)
        {
            int index = i + numJobEntities;
            var e = ecbMainThread.CreateEntity();
            RenderMeshUtility.AddComponents(
                e,
                ecbMainThread,
                desc);
            ecbMainThread.SetComponent(e, new LocalToWorld {Value = spawnJob.ComputeTransform(index)});
            // Use AddComponent because we didn't clone the prototype here
            ecbMainThread.AddComponent(e, new MaterialColor {Value = spawnJob.ComputeColor(index)});
        }

        spawnHandle.Complete();

        ecbJob.Playback(entityManager);
        ecbJob.Dispose();
        ecbMainThread.Playback(entityManager);
        entityManager.DestroyEntity(prototype);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
