using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class CreateSimpleTerrainScene : SceneCreationSettings {}

public class CreateSimpleTerrain : SceneCreationAuthoring<CreateSimpleTerrainScene> {}

public class CreateSimpleTerrainSystem : SceneCreationSystem<CreateSimpleTerrainScene>
{
    public override void CreateScene(CreateSimpleTerrainScene sceneSettings)
    {
        int2 size = new int2(2, 2);
        float3 scale = new float3(10, 1.0f, 10);
        NativeArray<float> heights = new NativeArray<float>(size.x * size.y * UnsafeUtility.SizeOf<float>(), Allocator.Temp);
        {
            heights[0] = 0;
            heights[1] = 0;
            heights[2] = 0;
            heights[3] = 0;
        }

        var collider = TerrainCollider.Create(heights, size, scale, TerrainCollider.CollisionMethod.VertexSamples);
        CreatedColliders.Add(collider);
        float3 position = new float3(15.0f, -1.0f, -5.0f);
        CreateTerrainBody(position, collider);

        // Mark this one CollisionResponse.None
        collider = TerrainCollider.Create(heights, size, scale, TerrainCollider.CollisionMethod.VertexSamples);
        CreatedColliders.Add(collider);
        unsafe
        {
            ((TerrainCollider*)collider.GetUnsafePtr())->Material.CollisionResponse = CollisionResponsePolicy.None;
        }
        position = new float3(15.0f, -1.0f, 10.0f);
        CreateTerrainBody(position, collider);
    }

    void CreateTerrainBody(float3 position, BlobAssetReference<Collider> collider)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity entity = entityManager.CreateEntity(new ComponentType[] {});

        entityManager.AddComponentData(entity, new LocalToWorld {});
        entityManager.AddComponentData(entity, new Translation { Value = position });
        entityManager.AddComponentData(entity, new Rotation { Value = quaternion.identity });

        var colliderComponent = new PhysicsCollider { Value = collider };
        entityManager.AddComponentData(entity, colliderComponent);

        CreateRenderMeshForCollider(entityManager, entity, collider, StaticMaterial);
    }
}
