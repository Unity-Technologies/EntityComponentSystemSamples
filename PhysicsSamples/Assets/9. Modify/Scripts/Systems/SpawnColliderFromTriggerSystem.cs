using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Material = Unity.Physics.Material;

// This class must use SystemBase because of the UnityEngine Materials and Meshes, and the RenderMeshArray
[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class SpawnColliderFromTriggerSystem : SystemBase
{
    private EntityQuery m_TriggerTilesCreateQuery;
    private EntityQuery m_MeshCreationResourcesQuery;

    private UnityEngine.Mesh engineMeshA;
    private UnityEngine.Mesh engineMeshB;  //also used by prototypeC

    private Entity prototypeA;
    private Entity prototypeB;
    private Entity prototypeC;
    private PhysicsCollider colliderA;
    private PhysicsCollider colliderB;
    private PhysicsCollider colliderC;
    private NativeList<BlobAssetReference<Unity.Physics.Collider>> CreatedColliderBlobs; //Must keep track of manually created blobs

    [BurstCompile]
    protected override void OnCreate()
    {
        CreatedColliderBlobs = new NativeList<BlobAssetReference<Unity.Physics.Collider>>(Allocator.Persistent);
        m_TriggerTilesCreateQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(TileTriggerCounter),
                typeof(PhysicsCollider),
                typeof(LocalTransform)
            },
        });

        // Get the RenderMeshArray data that was baked from CreateMeshFromResourcesAuthoring
        m_MeshCreationResourcesQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(RenderMeshArray),
                typeof(ResourcesLoadedTag)
            },
        });

        RequireForUpdate<TileTriggerCounter>();
        RequireForUpdate(m_MeshCreationResourcesQuery); // don't bother updating system if the mesh resources aren't there
    }

    [BurstCompile]
    protected override void OnStartRunning()
    {
        var meshResourcesEntities = m_MeshCreationResourcesQuery.ToEntityArray(Allocator.TempJob);
        if (meshResourcesEntities.Length == 0) return;

        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Data shared between the two prototypes:
        var worldIndex = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorldIndex;
        var renderMeshDescription = new RenderMeshDescription(UnityEngine.Rendering.ShadowCastingMode.Off);
        var meshResourcesEntity = meshResourcesEntities[0]; // Only care about the first one
        var renderMeshResources = entityManager.GetSharedComponentManaged<RenderMeshArray>(meshResourcesEntity);

        // Create Prototype A and test first utility function:
        prototypeA = entityManager.CreateEntity();
        ecb.AddSharedComponent<PhysicsWorldIndex>(prototypeA, worldIndex);

        // Populate base entity with the components required by Entities Graphics
        RenderMeshUtility.AddComponents(
            prototypeA,
            entityManager,
            renderMeshDescription,
            renderMeshResources,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        entityManager.AddComponentData(prototypeA, new LocalToWorld());

        engineMeshA = renderMeshResources.MeshReferences[0];
        var colliderBlob =
            Unity.Physics.MeshCollider.Create(engineMeshA, CollisionFilter.Default, Material.Default); // Test function
        CreatedColliderBlobs.Add(colliderBlob);
        colliderA = new PhysicsCollider() { Value = colliderBlob, };

        // Create Prototype B and test second utility function
        prototypeB = entityManager.CreateEntity();
        ecb.AddSharedComponent<PhysicsWorldIndex>(prototypeB, worldIndex);
        RenderMeshUtility.AddComponents(
            prototypeB,
            entityManager,
            renderMeshDescription,
            renderMeshResources,
            MaterialMeshInfo.FromRenderMeshArrayIndices(1, 1));
        entityManager.AddComponentData(prototypeB, new LocalToWorld());

        engineMeshB = renderMeshResources.MeshReferences[1];
        var engineMeshDataArray = UnityEngine.Mesh.AcquireReadOnlyMeshData(engineMeshB); //Test function
        colliderBlob =
            Unity.Physics.MeshCollider.Create(engineMeshDataArray, CollisionFilter.Default, Material.Default);
        CreatedColliderBlobs.Add(colliderBlob);
        colliderB = new PhysicsCollider() { Value = colliderBlob, };

        // Create Prototype C and test third utility function
        prototypeC = entityManager.CreateEntity();
        ecb.AddSharedComponent<PhysicsWorldIndex>(prototypeC, worldIndex);
        RenderMeshUtility.AddComponents(
            prototypeC,
            entityManager,
            renderMeshDescription,
            renderMeshResources,
            MaterialMeshInfo.FromRenderMeshArrayIndices(2, 2));
        entityManager.AddComponentData(prototypeC, new LocalToWorld());

        var engineMeshData = engineMeshDataArray[0];
        colliderBlob =
            Unity.Physics.MeshCollider.Create(engineMeshData, CollisionFilter.Default, Material.Default);
        CreatedColliderBlobs.Add(colliderBlob);
        colliderC = new PhysicsCollider() { Value = colliderBlob, };

        ecb.Playback(entityManager);
        ecb.Dispose();
        engineMeshDataArray.Dispose();
        meshResourcesEntities.Dispose();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (m_MeshCreationResourcesQuery.IsEmpty) return;

        if (prototypeA == Entity.Null || prototypeB == Entity.Null || prototypeC == Entity.Null)
        {
            Debug.Log("SpawnColliderFromTriggerSystem: Prototypes not initialized");
            return;
        }

        using (var entities = m_TriggerTilesCreateQuery.ToEntityArray(Allocator.TempJob))
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var tileInfo = m_TriggerTilesCreateQuery.ToComponentDataArray<TileTriggerCounter>(Allocator.TempJob);
            var tilePosition = m_TriggerTilesCreateQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var spawnJob = new SpawnCollidersFromTriggerJob()
            {
                PrototypeEntityA = prototypeA,
                PrototypeEntityB = prototypeB,
                PrototypeEntityC = prototypeC,
                ColliderA = colliderA,
                ColliderB = colliderB,
                ColliderC = colliderC,
                TileTriggerInfo = tileInfo,
                TilePosition = tilePosition,
                Ecb = ecb.AsParallelWriter(),
            }.Schedule(entities.Length, 128);
            spawnJob.Complete(); // this runs at the start of a frame, so we don't have a job dependency to wait on

            ecb.Playback(EntityManager);
            ecb.Dispose();
            tileInfo.Dispose(spawnJob);
            tilePosition.Dispose(spawnJob);
        }
    }

    [BurstCompile]
    struct SpawnCollidersFromTriggerJob : IJobParallelFor
    {
        public Entity PrototypeEntityA;                         // Entities to use as prototypes for the spawned entities
        public Entity PrototypeEntityB;
        public Entity PrototypeEntityC;
        public PhysicsCollider ColliderA;
        public PhysicsCollider ColliderB;
        public PhysicsCollider ColliderC;
        public NativeArray<TileTriggerCounter> TileTriggerInfo; // Need the tile trigger count and max count
        public NativeArray<LocalTransform> TilePosition;        // LocalTransform of the tile
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(int index)
        {
            var tiles = TileTriggerInfo[index];

            if (tiles.TriggerCount > 0 && tiles.TriggerCount < tiles.MaxTriggerCount)
            {
                Entity body;
                float3 verticalOffset;
                if (tiles.TriggerCount == 1)
                {
                    body = Ecb.Instantiate(index, PrototypeEntityA);
                    verticalOffset = new float3(0, 3, 0);
                    Ecb.AddComponent<PhysicsCollider>(index, body, ColliderA);  //Add physics collider created earlier
                }
                else if (tiles.TriggerCount == 2)
                {
                    body = Ecb.Instantiate(index, PrototypeEntityB);
                    verticalOffset = new float3(0, 4, 0);
                    Ecb.AddComponent<PhysicsCollider>(index, body, ColliderB);  //Add physics collider created earlier
                }
                else if (tiles.TriggerCount == 3)
                {
                    body = Ecb.Instantiate(index, PrototypeEntityC);
                    verticalOffset = new float3(0, 5, 0);
                    Ecb.AddComponent<PhysicsCollider>(index, body, ColliderC);  //Add physics collider created earlier
                }
                else
                {
                    return;
                }

                //Add transform, scale, localToWorld
                var position = TilePosition[index].Position + verticalOffset;   //Spawn above the tile of trigger event
                var tl = LocalTransform.FromPositionRotationScale(position, quaternion.identity, 0.25f);
                Ecb.AddComponent<LocalTransform>(index, body, tl);
                Ecb.AddComponent<LocalToWorld>(index, body, new LocalToWorld { Value = tl.ToMatrix() });

                //Note: mesh-mesh collider collisions are expensive. Keep static if possible (ref: SceneCreationSystem.CreateBody)
                bool isDynamic = false;
                if (isDynamic)
                {
                    Ecb.AddComponent(index, body, PhysicsMass.CreateDynamic(ColliderA.MassProperties, 5.0f));
                    Ecb.AddComponent<PhysicsVelocity>(index, body, new PhysicsVelocity
                    {
                        Linear = float3.zero,
                        Angular = float3.zero
                    });
                    Ecb.AddComponent(index, body, new PhysicsDamping
                    {
                        Linear = 0.01f,
                        Angular = 0.05f
                    });
                }
            }
        }
    }

    [BurstCompile]
    protected override void OnDestroy()
    {
        // Need to manually dispose all the BlobAssetReferences that were manually created
        foreach (var collider in CreatedColliderBlobs)
        {
            if (collider.IsCreated)
                collider.Dispose();
        }
        CreatedColliderBlobs.Dispose();

        EntityManager.DestroyEntity(prototypeA);
        EntityManager.DestroyEntity(prototypeB);
        EntityManager.DestroyEntity(prototypeC);
    }
}
