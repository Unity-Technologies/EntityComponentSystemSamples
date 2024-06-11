using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Material = UnityEngine.Material;

public abstract class SceneCreationSettings : IComponentData
{
    public Material DynamicMaterial;
    public Material StaticMaterial;
}

public class SceneCreatedTag : IComponentData
{
};

[UpdateInGroup(typeof(InitializationSystemGroup))]
public abstract partial class SceneCreationSystem<T> : SystemBase
    where T : SceneCreationSettings
{
    private EntityQuery m_ScenesToCreateQuery;

    protected Material DynamicMaterial;
    protected Material StaticMaterial;

    public NativeList<BlobAssetReference<Collider>> CreatedColliders;

    protected override void OnCreate()
    {
        CreatedColliders = new NativeList<BlobAssetReference<Collider>>(Allocator.Persistent);

        m_ScenesToCreateQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(T) },
            None = new ComponentType[] { typeof(SceneCreatedTag) },
        });
        RequireForUpdate<T>();
    }

    protected override void OnUpdate()
    {
        if (m_ScenesToCreateQuery.CalculateEntityCount() == 0) return;

        using (var entities = m_ScenesToCreateQuery.ToEntityArray(Allocator.TempJob))
        {
            foreach (Entity entity in entities)
            {
                T settings = EntityManager.GetComponentObject<T>(entity);
                DynamicMaterial = settings.DynamicMaterial;
                StaticMaterial = settings.StaticMaterial;

                CreateScene(settings);
                EntityManager.AddComponentData(entity, new SceneCreatedTag());
            }
        }
    }

    protected override void OnDestroy()
    {
        foreach (var collider in CreatedColliders)
        {
            if (collider.IsCreated)
                collider.Dispose();
        }

        CreatedColliders.Dispose();
    }

    public abstract void CreateScene(T sceneSettings);

    #region Utilities

    public static void CreateRenderMeshForCollider(
        EntityManager entityManager, Entity entity, BlobAssetReference<Collider> collider, Material material
    )
    {
        var mesh = collider.Value.ToMesh();
        var renderMesh = new RenderMeshArray(
            new[] { material },
            new[] { mesh });
        var renderMeshDescription = new RenderMeshDescription(UnityEngine.Rendering.ShadowCastingMode.Off);
        RenderMeshUtility.AddComponents(entity, entityManager, renderMeshDescription, renderMesh, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        entityManager.AddComponentData(entity, new LocalToWorld());
    }

    public Entity CreateBody(float3 position, quaternion orientation, BlobAssetReference<Collider> collider,
        float3 linearVelocity, float3 angularVelocity, float mass, bool isDynamic)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity entity = entityManager.CreateEntity(new ComponentType[] {});

        entityManager.AddComponentData(entity, new LocalToWorld {});

        entityManager.AddComponentData(entity, LocalTransform.FromPositionRotation(position, orientation));


        var colliderComponent = new PhysicsCollider { Value = collider };
        entityManager.AddComponentData(entity, colliderComponent);

        EntityManager.AddSharedComponent(entity, new PhysicsWorldIndex());

        CreateRenderMeshForCollider(entityManager, entity, collider, isDynamic ? DynamicMaterial : StaticMaterial);

        if (isDynamic)
        {
            entityManager.AddComponentData(entity, PhysicsMass.CreateDynamic(colliderComponent.MassProperties, mass));

            float3 angularVelocityLocal = math.mul(math.inverse(colliderComponent.MassProperties.MassDistribution.Transform.rot), angularVelocity);
            entityManager.AddComponentData(entity, new PhysicsVelocity
            {
                Linear = linearVelocity,
                Angular = angularVelocityLocal
            });
            entityManager.AddComponentData(entity, new PhysicsDamping
            {
                Linear = 0.01f,
                Angular = 0.05f
            });
        }

        return entity;
    }

    public Entity CreateStaticBody(float3 position, quaternion orientation, BlobAssetReference<Collider> collider)
    {
        return CreateBody(position, orientation, collider, float3.zero, float3.zero, 0.0f, false);
    }

    public Entity CreateDynamicBody(float3 position, quaternion orientation, BlobAssetReference<Collider> collider,
        float3 linearVelocity, float3 angularVelocity, float mass)
    {
        return CreateBody(position, orientation, collider, linearVelocity, angularVelocity, mass, true);
    }

    public Entity CreateJoint(PhysicsJoint joint, Entity entityA, Entity entityB, bool enableCollision = false)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        ComponentType[] componentTypes =
        {
            typeof(PhysicsConstrainedBodyPair),
            typeof(PhysicsJoint)
        };
        Entity jointEntity = entityManager.CreateEntity(componentTypes);

        entityManager.SetComponentData(jointEntity, new PhysicsConstrainedBodyPair(entityA, entityB, enableCollision));
        entityManager.SetComponentData(jointEntity, joint);

        EntityManager.AddSharedComponent(jointEntity, new PhysicsWorldIndex());

        return jointEntity;
    }

    public static RigidTransform GetBodyTransform(Entity entity)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
        return new RigidTransform(
            localTransform.Rotation,
            localTransform.Position);
    }

    #endregion
}
