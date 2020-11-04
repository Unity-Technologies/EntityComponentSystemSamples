using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;

public abstract class SceneCreationSettings : IComponentData
{
    public Material DynamicMaterial;
    public Material StaticMaterial;
}

public class SceneCreatedTag : IComponentData {};


// Base class of authoring components that create scene from code, using SceneCreationSystem
public abstract class SceneCreationAuthoring<T> : MonoBehaviour, IConvertGameObjectToEntity
    where T : SceneCreationSettings, new()
{
    public Material DynamicMaterial;
    public Material StaticMaterial;

    public virtual void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        T sceneSettings = new T
        {
            DynamicMaterial = DynamicMaterial,
            StaticMaterial = StaticMaterial
        };
        dstManager.AddComponentData(entity, sceneSettings);
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public abstract class SceneCreationSystem<T> : SystemBase
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
        RequireForUpdate(GetEntityQuery(new ComponentType[] { typeof(T) }));
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

    static readonly Type k_DrawComponent = typeof(Unity.Physics.Authoring.DisplayBodyColliders)
        .GetNestedType("DrawComponent", BindingFlags.NonPublic);

    static readonly MethodInfo k_DrawComponent_BuildDebugDisplayMesh = k_DrawComponent
        .GetMethod("BuildDebugDisplayMesh", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(BlobAssetReference<Collider>) }, null);

    static readonly Type k_DisplayResult = k_DrawComponent.GetNestedType("DisplayResult");

    static readonly FieldInfo k_DisplayResultsMesh = k_DisplayResult.GetField("Mesh");
    static readonly PropertyInfo k_DisplayResultsTransform = k_DisplayResult.GetProperty("Transform");

    public static void CreateRenderMeshForCollider(
        EntityManager entityManager, Entity entity, BlobAssetReference<Collider> collider, Material material
    )
    {
        var mesh = new Mesh { hideFlags = HideFlags.DontSave };
        var instances = new List<CombineInstance>(8);
        var numVertices = 0;
        foreach (var displayResult in (IEnumerable)k_DrawComponent_BuildDebugDisplayMesh.Invoke(null, new object[] { collider }))
        {
            var instance = new CombineInstance
            {
                mesh = k_DisplayResultsMesh.GetValue(displayResult) as Mesh,
                transform = (float4x4)k_DisplayResultsTransform.GetValue(displayResult)
            };
            instances.Add(instance);
            numVertices += mesh.vertexCount;
        }
        mesh.indexFormat = numVertices > UInt16.MaxValue ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.CombineMeshes(instances.ToArray());
        mesh.RecalculateBounds();

        entityManager.AddSharedComponentData(entity, new RenderMesh
        {
            mesh = mesh,
            material = material
        });
        entityManager.AddComponentData(entity, new RenderBounds { Value = mesh.bounds.ToAABB() });
    }

    public Entity CreateBody(float3 position, quaternion orientation, BlobAssetReference<Collider> collider,
        float3 linearVelocity, float3 angularVelocity, float mass, bool isDynamic)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity entity = entityManager.CreateEntity(new ComponentType[] {});

        entityManager.AddComponentData(entity, new LocalToWorld {});
        entityManager.AddComponentData(entity, new Translation { Value = position });
        entityManager.AddComponentData(entity, new Rotation { Value = orientation });

        var colliderComponent = new PhysicsCollider { Value = collider };
        entityManager.AddComponentData(entity, colliderComponent);

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

        return jointEntity;
    }

    public static RigidTransform GetBodyTransform(Entity entity)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        return new RigidTransform(
            entityManager.GetComponentData<Rotation>(entity).Value,
            entityManager.GetComponentData<Translation>(entity).Value);
    }

    #endregion
}
