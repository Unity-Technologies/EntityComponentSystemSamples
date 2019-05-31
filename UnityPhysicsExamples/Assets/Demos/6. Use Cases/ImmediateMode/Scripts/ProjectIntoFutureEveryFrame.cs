using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;


public class ProjectIntoFutureEveryFrame : MonoBehaviour
{
    public Mesh referenceMesh;
    public Material referenceMaterial;
    public int numSteps = 25;

    private RenderMesh ghostMaterial;

    // Use this for initialization
    void Start ()
    {
        ghostMaterial = new RenderMesh
        {
            mesh = referenceMesh,
            material = referenceMaterial
        };
    }

    void createTrails(PhysicsWorld localWorld, Color color)
    {
        //UnityEngine.Material material = new UnityEngine.Material(Shader.Find("Lightweight-Default"));
        //material.color = color;
        var em = World.Active.EntityManager;

        const float minVelocitySq = 0.1f;
        for( int i = 0; i < localWorld.DynamicBodies.Length; i++ )
        {
            if (math.lengthsq(localWorld.MotionVelocities[i].LinearVelocity) > minVelocitySq)
            {
                var body = localWorld.DynamicBodies[i];

                var ghost = em.Instantiate(body.Entity);

                em.RemoveComponent<PhysicsCollider>(ghost);
                em.RemoveComponent<PhysicsVelocity>(ghost);

                em.AddComponentData(ghost, new EntityKiller() { TimeToDie = 2 });

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

    // Update is called once per frame
    void Update()
    {
        ref PhysicsWorld world = ref World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

        var localWorld = (PhysicsWorld)world.Clone();
        var simulation = new Simulation();
        var stepInput = new SimulationStepInput
        {
            World = localWorld,
            TimeStep = Time.fixedDeltaTime,
            ThreadCountHint = PhysicsStep.Default.ThreadCountHint,
            Gravity = math.up() * -9.81f,
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
                    createTrails(localWorld, color);
                }

                color.a = 1.0f - ((float)i / numSteps);
            }
        }
        finally
        {
            localWorld.Dispose();
        }

        simulation.Dispose();
    }
}
