using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using System.Collections.Generic;

public struct ProjectIntoFutureTrail : IComponentData { }

public class ProjectIntoFutureOnCue : MonoBehaviour, IRecieveEntity
{
    public Mesh referenceMesh;
    public Material referenceMaterial;
    public int numSteps = 25;

    public Slider rotateSlider;
    public Slider strengthSlider;

    public Entity WhiteBallEntity = Entity.Null;

    public bool NeedUpdate = true;

    private RenderMesh ghostMaterial;

    // Use this for initialization
    void Start()
    {
        ghostMaterial = new RenderMesh
        {
            mesh = referenceMesh,
            material = referenceMaterial
        };
    }

    void clearTrails()
    {
        var em = World.Active.EntityManager;
        var eA = em.GetAllEntities(Allocator.Temp);
        foreach (var e in eA)
        {
            if (em.HasComponent<ProjectIntoFutureTrail>(e))
            {
                em.DestroyEntity(e);
            }
        }
        eA.Dispose();
    }

    void createTrails(ref PhysicsWorld localWorld, Color color)
    {
        //UnityEngine.Material material = new UnityEngine.Material(Shader.Find("Lightweight-Default"));
        //material.color = color;
        var em = World.Active.EntityManager;
        const float minVelocitySq = 0.05f;
        for (int i = 0; i < localWorld.DynamicBodies.Length; i++)
        {
            if (math.lengthsq(localWorld.MotionVelocities[i].LinearVelocity) > minVelocitySq)
            {
                var body = localWorld.DynamicBodies[i];

                var ghost = em.Instantiate(body.Entity);

                em.RemoveComponent<PhysicsCollider>(ghost);
                em.RemoveComponent<PhysicsVelocity>(ghost);

                em.AddComponentData(ghost, new ProjectIntoFutureTrail() );

                em.SetSharedComponentData(ghost, ghostMaterial);

                Translation position = em.GetComponentData<Translation>(ghost);
                position.Value = body.WorldFromBody.pos;
                em.SetComponentData(ghost, position);

                Rotation rotation = em.GetComponentData<Rotation>(ghost);
                rotation.Value = body.WorldFromBody.rot;
                em.SetComponentData(ghost, rotation);

                var scale = new NonUniformScale() { Value = 0.05f };
                if (em.HasComponent<NonUniformScale>(ghost))
                {
                    scale.Value *= em.GetComponentData<NonUniformScale>(ghost).Value;
                    em.SetComponentData(ghost, scale);
                }
                else
                {
                    em.AddComponentData(ghost, scale);
                }
            }
        }
    }

    private float3 GetVelocityFromSliders()
    {
        float angle = rotateSlider.value - 90;
        float strength = strengthSlider.value;
        float3 velocity = strength * math.forward(quaternion.AxisAngle(math.up(), math.radians(angle)));

        return velocity;
    }

    // Update is called once per frame
    void UpdateTrails()
    {
        if (WhiteBallEntity == null)
        {
            return;
        }

        ref PhysicsWorld world = ref World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

        var localWorld = (PhysicsWorld)world.Clone();
        localWorld.SetLinearVelocity(world.GetRigidBodyIndex(WhiteBallEntity), GetVelocityFromSliders());

        var simulation = new Simulation();
        // TODO: get these setting from the current simulation setup
        var stepInput = new SimulationStepInput
        {
            World = localWorld,
            TimeStep = Time.fixedDeltaTime,
            ThreadCountHint = Unity.Physics.PhysicsStep.Default.ThreadCountHint,
            Gravity = Unity.Physics.PhysicsStep.Default.Gravity,
            SynchronizeCollisionWorld = true
        };

        try
        {
            // Sync the collision world first
            localWorld.CollisionWorld.ScheduleUpdateDynamicLayer(ref localWorld, stepInput.TimeStep, stepInput.ThreadCountHint, new JobHandle()).Complete();

            Color color = Color.red;
            for (int i = 0; i < numSteps; i++)
            {
                simulation.Step(stepInput);

                if (i > 0)
                {
                    createTrails(ref localWorld, color);
                }

                color.a = 1.0f - ((float)i / numSteps);
            }
        }
        finally
        {
            localWorld.Dispose();
            simulation.Dispose();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (NeedUpdate)
        {
            clearTrails();

            UpdateTrails();

            NeedUpdate = false;
        }
    }



    public void OnSliderValueChanged()
    {
        NeedUpdate = true;
    }

    public void OnButtonClick()
    {
        if (WhiteBallEntity != Entity.Null)
        {
            WhiteBallEntity.SetLinearVelocity(GetVelocityFromSliders());
            clearTrails();
            NeedUpdate = false;
        }
    }

    public void SetRecievedEntity(Entity entity)
    {
        WhiteBallEntity = entity;
    }
}
