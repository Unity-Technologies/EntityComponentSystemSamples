using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class AddComponentsExample : MonoBehaviour
{
    public Mesh Mesh;
    public Material m_material;
    public bool m_differentMaterial = false;

    public int m_w = 30;
    public int m_h = 30;


    // Example Burst job that creates many entities
    [GenerateTestsForBurstCompatibility]
    public struct SpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public int w;
        public int h;
        public bool singleMat;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(int index)
        {
            // Clone the Prototype entity to create a new entity.
            var e = Ecb.Instantiate(index, Prototype);
            // Prototype has all correct components up front, can use SetComponent to
            // set values unique to the newly created entity, such as the transform.
            int matIndex = singleMat ? 0 : index;
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(matIndex, 0));
            Ecb.SetComponent(index, e, new LocalToWorld {Value = ComputeTransform(index)});
        }

        public float4x4 ComputeTransform(int index)
        {
            int y = index / w;
            int x = index % h;
            float3 pos = new float3(x - (float)w * 0.5f, 0, y - (float)h * 0.5f);

            return float4x4.Translate(pos);
        }
    }

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Create a RenderMeshDescription using the convenience constructor
        // with named parameters.
        var desc = new RenderMeshDescription(
            shadowCastingMode: ShadowCastingMode.Off,
            receiveShadows: false);

        int objCount = m_w * m_h;
        var matList = new List<UnityObjectRef<Material>>();
        if ( m_differentMaterial )
        {
            for (int i=0;i<objCount;i++)
            {
                var mat = new Material(m_material);
                Color col = Color.HSVToRGB(((float)(i * 10) / (float)objCount) % 1.0f, 0.7f, 1.0f);
                //                Color col = Color.HSVToRGB(Random.Range(0.0f,1.0f), 1.0f, 1.0f);
                mat.SetColor("_Color", col);              // set for LW
                mat.SetColor("_BaseColor", col);          // set for HD
                matList.Add(mat);
            }
        }
        else
        {
            matList.Add(m_material);
        }



        var meshArray = new RenderMeshArray() { MaterialReferences = matList.ToArray(), MeshReferences = new[] { (UnityObjectRef<Mesh>)Mesh } };
        meshArray.ResetHash128();

        // Create empty base entity
        var prototype = entityManager.CreateEntity();

        // Call AddComponents to populate base entity with the components required
        // by Hybrid Renderer
        RenderMeshUtility.AddComponents(
            prototype,
            entityManager,
            desc,
            meshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        entityManager.AddComponentData(prototype, new LocalToWorld());

        // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
        // which can be either a Prefab or an entity created at run time like in this sample.
        // This is the fastest and most efficient way to create entities at run time.
        var spawnJob = new SpawnJob
        {
            Prototype = prototype,
            Ecb = ecb.AsParallelWriter(),
            w = m_w,
            h = m_h,
            singleMat = !m_differentMaterial
        };

        var spawnHandle = spawnJob.Schedule(m_h*m_w,128);
        spawnHandle.Complete();

        ecb.Playback(entityManager);
        ecb.Dispose();
        entityManager.DestroyEntity(prototype);
    }
}
