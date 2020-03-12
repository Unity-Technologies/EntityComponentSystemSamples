using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;

public struct ProjectIntoFutureTrail : IComponentData { }

[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class ProjectIntoFutureOnCueSystem : JobComponentSystem
{
    private bool NeedsUpdate = true;
    private int m_NumSteps = 0;
    public int NumSteps { get => m_NumSteps; set { m_NumSteps = value; NeedsUpdate = true; } }
    private float3 m_WhiteBallVelocity = 0f;
    public float3 WhiteBallVelocity { get => m_WhiteBallVelocity; set { m_WhiteBallVelocity = value; NeedsUpdate = true; } }

    private Entity WhiteBallEntity = Entity.Null;
    private RenderMesh GhostMaterial;
    private float GhostScale = 0.01f;

    public bool IsInitialized => !WhiteBallEntity.Equals(Entity.Null);

    private NativeArray<float3> Positions;
    private PhysicsWorld LocalWorld;
    private SimulationContext SimulationContext;

    private JobHandle FinalJobHandle;

    public void Initialize(Entity whiteBall, int numSteps, Mesh referenceMesh, Material referenceMaterial, ref PhysicsWorld world)
    {
        WhiteBallEntity = whiteBall;
        NumSteps = numSteps;
        GhostMaterial = new RenderMesh
        {
            mesh = referenceMesh,
            material = referenceMaterial
        };

        CheckEntityPool(world.NumDynamicBodies);
    }

    public void ClearTrails()
    {
        JobHandle handle = new JobHandle();
        handle = new ResetPositionsJob { Positions = Positions }.Schedule(handle);
        handle = new UpdateTrailEntityPositionsJob { NewPositions = Positions, NumSteps = NumSteps, GhostScale = GhostScale }.Schedule(this, handle);
        handle.Complete();
    }

    public void CheckEntityPool(int numDynamicBodies)
    {
        int totalNumberOfEntities = NumSteps * numDynamicBodies;
        int diff = totalNumberOfEntities - Positions.Length;

        if (diff <= 0)
        {
            return;
        }

        var manager = World.EntityManager;

        if (Positions.IsCreated) Positions.Dispose();
        Positions = new NativeArray<float3>(totalNumberOfEntities, Allocator.Persistent);

        for (int i = 0; i < diff; i++)
        {
            var ghost = manager.Instantiate(WhiteBallEntity);

            manager.RemoveComponent<PhysicsCollider>(ghost);
            manager.RemoveComponent<PhysicsVelocity>(ghost);

            manager.AddComponentData(ghost, new ProjectIntoFutureTrail());
            manager.SetSharedComponentData(ghost, GhostMaterial);

            var scale = new NonUniformScale { Value = GhostScale };
            manager.AddComponentData(ghost, scale);
        }

        NeedsUpdate = true;
    }

    // "Hides" entities to some position not visible to player.
    // More efficient than removing rendering component from entities.
    [BurstCompile]
    struct ResetPositionsJob : IJob
    {
        public NativeArray<float3> Positions;

        public void Execute()
        {
            for (int i = 0; i < Positions.Length; i++)
            {
                Positions[i] = new float3(0, -1, 0);
            }
        }
    }

    [BurstCompile]
    struct UpdateTrailEntityPositionsJob : IJobForEachWithEntity<Translation, Rotation, NonUniformScale, ProjectIntoFutureTrail>
    {
        [ReadOnly] public NativeArray<float3> NewPositions;
        [ReadOnly] public int NumSteps;
        [ReadOnly] public float GhostScale;

        public void Execute(Entity entity, int index, 
            ref Translation t, ref Rotation r, ref NonUniformScale s,
            [ReadOnly] ref ProjectIntoFutureTrail p)
        {
            var posT0 = NewPositions[index];

            // Return if we are on the last step
            if ((index % NumSteps) == (NumSteps - 1))
            {
                t.Value = posT0;
                s.Value = GhostScale;
                return;
            }

            // Get the next position
            var posT1 = NewPositions[index+1];

            // Return if we haven't moved
            var haveMovement = !posT0.Equals(posT1);
            if (!haveMovement)
            {
                t.Value = posT0; // Comment this out to leave the trails after shot.
                s.Value = GhostScale;
                return;
            }

            // Position the ghost ball half way between T0 and T1
            t.Value = math.lerp(posT0, posT1, 0.5f);

            // Orientation the ball along the direction between T0 and T1
            // and stretch the ball between those 2 positions.
            var forward = posT1 - posT0;
            var scale = math.length(forward);
            var rotation = quaternion.LookRotationSafe(forward, new float3(0, 1, 0));

            r.Value = rotation;
            s.Value = new float3(s.Value.x, s.Value.y, scale);
        }
    }

    [BurstCompile]
    struct StepLocalWorldJob : IJob
    {
        public SimulationStepInput StepInput;
        public SimulationContext SimulationContext;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> TrailPositions;
        public int NumSteps;
        public int StepIndex;

        public void Execute()
        {
            // Update the trails
            for (int b = 0; b < StepInput.World.DynamicBodies.Length; b++)
            {
                TrailPositions[b * NumSteps + StepIndex] = StepInput.World.DynamicBodies[b].WorldFromBody.pos;
            }
            
            // Step the local world
            Simulation.StepImmediate(StepInput, ref SimulationContext);
        }
    }

    protected override void OnCreate()
    {
        Positions = new NativeArray<float3>();
        LocalWorld = new PhysicsWorld();
        SimulationContext = new SimulationContext();
        SimulationContext.Reset(ref LocalWorld);

        FinalJobHandle = new JobHandle();
    }

    protected override void OnDestroy()
    {
        if (Positions.IsCreated) Positions.Dispose();
        if (LocalWorld.NumBodies != 0) LocalWorld.Dispose();
        
        SimulationContext.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        bool bUpdate = true;
        bUpdate &= (IsInitialized && NeedsUpdate);
        bUpdate &= !WhiteBallVelocity.Equals(float3.zero);
        if (!bUpdate)
        {
            return inputDeps;
        }

        var jobHandle = inputDeps;

        var buildPhysics = BasePhysicsDemo.DefaultWorld.GetExistingSystem<BuildPhysicsWorld>();
        ref var world = ref buildPhysics.PhysicsWorld;
        CheckEntityPool(world.NumDynamicBodies);

        // Complete the local simulation trails from the previous step.
        FinalJobHandle.Complete();

        // Clear the trails ready for a new simulation prediction
        jobHandle = new ResetPositionsJob { Positions = Positions }.Schedule(jobHandle);

        // If a local world was previously cloned get rid of it and make a new one.
        if (LocalWorld.NumBodies > 0)
        {
            LocalWorld.Dispose();
        }
        LocalWorld = world.Clone();

#if !UNITY_DOTSPLAYER
        float timeStep = UnityEngine.Time.fixedDeltaTime;
#else
        float timeStep = Time.DeltaTime;
#endif
        var stepInput = new SimulationStepInput
        {
            World = LocalWorld,
            TimeStep = timeStep,
            NumSolverIterations = Unity.Physics.PhysicsStep.Default.SolverIterationCount,
            Gravity = Unity.Physics.PhysicsStep.Default.Gravity,
            SynchronizeCollisionWorld = true,
        };

        // Assign the requested cue ball velocity to the local simulation
        LocalWorld.SetLinearVelocity(LocalWorld.GetRigidBodyIndex(WhiteBallEntity), WhiteBallVelocity);

        // Sync the CollisionWorld before the initial step. 
        // As stepInput.SynchronizeCollisionWorld is true the simulation will
        // automatically sync the CollisionWorld on subsequent steps.
        // This is only needed as we have modified the cue ball velocity.
        jobHandle = LocalWorld.CollisionWorld.ScheduleUpdateDynamicTree(
            ref LocalWorld, stepInput.TimeStep, stepInput.Gravity, jobHandle);
        
        // NOTE: Currently the advice is to not chain local simulation steps.
        // Therefore we complete necessary work here and at each step.
        jobHandle.Complete();

        // Step the local world
        for (int i = 0; i < NumSteps; i++)
        {
            // TODO: look into a public version of SimulationContext.ScheduleReset
            // so that we can chain multiple StepLocalWorldJob instances.

            // Dispose and reallocate input velocity buffer, if dynamic body count has increased.
            // Dispose previous collision and trigger event streams and allocator new streams. 
            SimulationContext.Reset(ref stepInput.World);

            // Step the local world and complete the job.
            new StepLocalWorldJob()
            {
                StepInput = stepInput,
                SimulationContext = SimulationContext,
                StepIndex = i,
                NumSteps = NumSteps,
                TrailPositions = Positions
            }.Schedule(jobHandle).Complete();
        }

        jobHandle = new UpdateTrailEntityPositionsJob
        {
            NewPositions = Positions,
            NumSteps = NumSteps,
            GhostScale = GhostScale,
        }.Schedule(this, jobHandle);

        NeedsUpdate = false;

        FinalJobHandle = jobHandle;
        return FinalJobHandle;
    }
}

public class ProjectIntoFutureOnCue : MonoBehaviour, IReceiveEntity
{
    public Mesh ReferenceMesh;
    public Material ReferenceMaterial;
    public Slider RotateSlider;
    public Slider StrengthSlider;
    public int NumSteps = 25;

    private Entity WhiteBallEntity = Entity.Null;
    private ProjectIntoFutureOnCueSystem System;

    private float3 GetVelocityFromSliders()
    {
        float angle = RotateSlider.value - 90;
        float strength = StrengthSlider.value;
        float3 velocity = strength * math.forward(quaternion.AxisAngle(math.up(), math.radians(angle)));

        return velocity;
    }

    void Start()
    {
        System = BasePhysicsDemo.DefaultWorld.GetOrCreateSystem<ProjectIntoFutureOnCueSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!System.IsInitialized && !WhiteBallEntity.Equals(Entity.Null))
        {
            var physicsWorld = BasePhysicsDemo.DefaultWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
            if (physicsWorld.NumDynamicBodies > 0)
            {
                System.Initialize(WhiteBallEntity, NumSteps, ReferenceMesh, ReferenceMaterial, ref physicsWorld);
                System.WhiteBallVelocity = GetVelocityFromSliders();
            }
        }
    }

    public void OnSliderValueChanged()
    {
        if (System != null && System.IsInitialized)
        {
            System.WhiteBallVelocity = GetVelocityFromSliders();

            // Cull velocity on all the balls so that the simulation
            // will match the local prediction
            var entityManager = BasePhysicsDemo.DefaultWorld.EntityManager;
            using (var entities = entityManager.GetAllEntities())
            {
                foreach (var entity in entities)
                {
                    if (entityManager.HasComponent<PhysicsVelocity>(entity))
                    {
                        var velocity = entityManager.GetComponentData<PhysicsVelocity>(entity);
                        velocity.Linear = float3.zero;
                        velocity.Angular = float3.zero;
                        entityManager.SetComponentData(entity, velocity);
                    }
                }
            }
        }
    }

    public void OnButtonClick()
    {
        if (System.IsInitialized)
        {
            var entityManager = BasePhysicsDemo.DefaultWorld.EntityManager;

            // assign the required velocity to the white ball in the main simulation
            var velocity = entityManager.GetComponentData<PhysicsVelocity>(WhiteBallEntity);
            velocity.Linear = GetVelocityFromSliders();
            entityManager.SetComponentData(WhiteBallEntity, velocity);

            System.WhiteBallVelocity = float3.zero;
        }
    }

    public void SetReceivedEntity(Entity entity)
    {
        WhiteBallEntity = entity;
    }
}
