using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using Material = UnityEngine.Material;

public class ProjectIntoFutureGO : MonoBehaviour
{

    public Material referenceMaterial;
    public int numSteps = 25;

    // Use this for initialization
    void Start () {
		
	}

    void cleanUpTrails()
    {
        for( int i = 0; i < gameObject.transform.childCount; i++ )
        {
            Destroy(gameObject.transform.GetChild(i).gameObject);
        }
    }
    void createTrails(PhysicsWorld localWorld, Color color)
    {
        //UnityEngine.Material material = new UnityEngine.Material(Shader.Find("Lightweight-Default"));
        //material.color = color;

        foreach( var body in localWorld.DynamicBodies )
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            //go.GetComponent<Renderer>().material = material;
            go.transform.position = new Vector3(
                                            body.WorldFromBody.pos.x, 
                                            body.WorldFromBody.pos.y, 
                                            body.WorldFromBody.pos.z);
            go.transform.rotation = new Quaternion(
                                            body.WorldFromBody.rot.value.x, 
                                            body.WorldFromBody.rot.value.y, 
                                            body.WorldFromBody.rot.value.z, 
                                            body.WorldFromBody.rot.value.w);
            go.transform.SetParent(transform, true);
        }
    }

    // Update is called once per frame
    void Update ()
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

        cleanUpTrails();
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

//[UpdateAfter(typeof(Physics.Systems.UpdatePhysicsWorld)), UpdateBefore(typeof(Physics.Systems.PhysicsEndSystem))]
//public class ProjectIntoFutureSystem: ComponentSystem
//{

//}
