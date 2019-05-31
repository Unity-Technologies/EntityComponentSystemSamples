using System.Collections.Generic;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Entities;
using Unity.Mathematics;
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
    public static EntityManager EntityManager => World.Active.EntityManager;
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
        EntityManager entityManager = EntityManager;
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

    private Entity CreateBody(float3 position, quaternion orientation, BlobAssetReference<Collider> collider,
        float3 linearVelocity, float3 angularVelocity, float mass, bool isDynamic)
    {
        EntityManager entityManager = EntityManager;

        Entity entity = entityManager.CreateEntity(new ComponentType[] { });

        entityManager.AddComponentData(entity, new LocalToWorld { });
        entityManager.AddComponentData(entity, new Translation { Value = position });
        entityManager.AddComponentData(entity, new Rotation { Value = orientation });

        var colliderComponent = new PhysicsCollider { Value = collider };
        entityManager.AddComponentData(entity, colliderComponent);

        Mesh mesh = new Mesh();
        List<Unity.Physics.Authoring.DisplayBodyColliders.DrawComponent.DisplayResult> meshes;
        unsafe { meshes = Unity.Physics.Authoring.DisplayBodyColliders.DrawComponent.BuildDebugDisplayMesh(colliderComponent.ColliderPtr); }
        CombineInstance[] instances = new CombineInstance[meshes.Count];
        for (int i = 0; i < meshes.Count; i++)
        {
            instances[i] = new CombineInstance
            {
                mesh = meshes[i].Mesh,
                transform = Matrix4x4.TRS(meshes[i].Position, meshes[i].Orientation, meshes[i].Scale)
            };
        }
        mesh.CombineMeshes(instances);

        entityManager.AddSharedComponentData(entity, new RenderMesh
        {
            mesh = mesh,
            material = isDynamic ? dynamicMaterial : staticMaterial
        });

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
        EntityManager entityManager = World.Active.EntityManager;
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
        EntityManager.SetComponentData<Unity.Physics.Authoring.PhysicsDebugDisplayData>(stepper, debugDisplay);
    }

    protected RigidTransform GetBodyTransform(Entity entity)
    {
        EntityManager entityManager = EntityManager;
        return new RigidTransform(
            entityManager.GetComponentData<Rotation>(entity).Value,
            entityManager.GetComponentData<Translation>(entity).Value);
    }
}
