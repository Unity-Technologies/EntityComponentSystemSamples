using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;

/// <summary>
/// Helper for demos set up in C# rather than in the editor
/// </summary>
public class BasePhysicsDemo : MonoBehaviour
{
    public static World DefaultWorld => World.DefaultGameObjectInjectionWorld;

    protected Entity stepper;

    public Material dynamicMaterial;
    public Material staticMaterial;

    public SimulationType StepType = SimulationType.UnityPhysics;

    protected void init(float3 gravity)
    {
        // Camera control
        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        CameraControl cameraControl = mainCamera.AddComponent<CameraControl>();
        cameraControl.lookSpeedH = 0.6f;
        cameraControl.lookSpeedV = 0.6f;
        cameraControl.zoomSpeed = 1.0f;
        cameraControl.dragSpeed = 4.0f;

        // Create the stepper
        var entityManager = DefaultWorld.EntityManager;
        ComponentType[] componentTypes = {
            typeof(PhysicsStep),
            typeof(Unity.Physics.Authoring.PhysicsDebugDisplayData),
            typeof(MousePick)
        };
        stepper = entityManager.CreateEntity(componentTypes);
        entityManager.SetComponentData(stepper, new PhysicsStep
        {
            SimulationType = StepType,
            Gravity = gravity,
            SolverIterationCount = PhysicsStep.Default.SolverIterationCount,
            ThreadCountHint = PhysicsStep.Default.ThreadCountHint
        });
        // Add options for visually debugging physics information
        entityManager.SetComponentData(stepper, new Unity.Physics.Authoring.PhysicsDebugDisplayData { });


        // Load assets
        //dynamicMaterial = (Material)Resources.Load("Materials/PhysicsDynamicMaterial");
        //staticMaterial = (Material)Resources.Load("Materials/PhysicsStaticMaterial");
    }

    protected virtual void Start()
    {
        init(new float3(0, -9.81f, 0));
    }

    //
    // Object creation
    //

    // TODO: add proper utility APIs for converting Collider into buffers usable for UnityEngine.Mesh and for drawing lines
    static readonly Type k_DrawComponent = typeof(Unity.Physics.Authoring.DisplayBodyColliders)
        .GetNestedType("DrawComponent", BindingFlags.NonPublic);

    static readonly MethodInfo k_DrawComponent_BuildDebugDisplayMesh = k_DrawComponent
        .GetMethod("BuildDebugDisplayMesh", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(BlobAssetReference<Collider>) }, null);

    static readonly Type k_DisplayResult = k_DrawComponent.GetNestedType("DisplayResult");

    static readonly FieldInfo k_DisplayResultsMesh = k_DisplayResult.GetField("Mesh");
    static readonly PropertyInfo k_DisplayResultsTransform = k_DisplayResult.GetProperty("Transform");

    Entity CreateBody(float3 position, quaternion orientation, BlobAssetReference<Collider> collider,
        float3 linearVelocity, float3 angularVelocity, float mass, bool isDynamic)
    {
        var entityManager = DefaultWorld.EntityManager;

        Entity entity = entityManager.CreateEntity(new ComponentType[] { });

        entityManager.AddComponentData(entity, new LocalToWorld { });
        entityManager.AddComponentData(entity, new Translation { Value = position });
        entityManager.AddComponentData(entity, new Rotation { Value = orientation });

        var colliderComponent = new PhysicsCollider { Value = collider };
        entityManager.AddComponentData(entity, colliderComponent);

        var mesh = new Mesh();
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
            material = isDynamic ? dynamicMaterial : staticMaterial
        });
        entityManager.AddComponentData(entity, new RenderBounds { Value = mesh.bounds.ToAABB() });

        if (isDynamic)
        {
            entityManager.AddComponentData(entity, PhysicsMass.CreateDynamic(colliderComponent.MassProperties, mass));

            float3 angularVelocityLocal = math.mul(math.inverse(colliderComponent.MassProperties.MassDistribution.Transform.rot), angularVelocity);
            entityManager.AddComponentData(entity, new PhysicsVelocity()
            {
                Linear = linearVelocity,
                Angular = angularVelocityLocal
            });
            entityManager.AddComponentData(entity, new PhysicsDamping()
            {
                Linear = 0.01f,
                Angular = 0.05f
            });
        }

        return entity;
    }

    protected Entity CreateStaticBody(float3 position, quaternion orientation, BlobAssetReference<Collider> collider)
    {
        return CreateBody(position, orientation, collider, float3.zero, float3.zero, 0.0f, false);
    }

    protected Entity CreateDynamicBody(float3 position, quaternion orientation, BlobAssetReference<Collider> collider,
        float3 linearVelocity, float3 angularVelocity, float mass)
    {
        return CreateBody(position, orientation, collider, linearVelocity, angularVelocity, mass, true);
    }

    protected unsafe Entity CreateJoint(BlobAssetReference<JointData> jointData, Entity entityA, Entity entityB, bool enableCollision = false)
    {
        var entityManager = DefaultWorld.EntityManager;
        ComponentType[] componentTypes = new ComponentType[1];
        componentTypes[0] = typeof(PhysicsJoint);
        Entity jointEntity = entityManager.CreateEntity(componentTypes);
        entityManager.SetComponentData(jointEntity, new PhysicsJoint
        {
            JointData = jointData,
            EntityA = entityA,
            EntityB = entityB,
            EnableCollision = (enableCollision ? 1 : 0)
        });
        return jointEntity;
    }

    //
    // Helper methods
    //

    protected void SetDebugDisplay(Unity.Physics.Authoring.PhysicsDebugDisplayData debugDisplay)
    {
        var entityManager = DefaultWorld.EntityManager;
        entityManager.SetComponentData<Unity.Physics.Authoring.PhysicsDebugDisplayData>(stepper, debugDisplay);
    }

    protected RigidTransform GetBodyTransform(Entity entity)
    {
        var entityManager = DefaultWorld.EntityManager;
        return new RigidTransform(
            entityManager.GetComponentData<Rotation>(entity).Value,
            entityManager.GetComponentData<Translation>(entity).Value);
    }
}