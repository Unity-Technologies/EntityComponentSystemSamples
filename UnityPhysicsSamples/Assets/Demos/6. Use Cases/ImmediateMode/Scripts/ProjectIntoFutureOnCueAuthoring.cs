using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using Slider = UnityEngine.UI.Slider;

public struct ProjectIntoFutureTrail : IComponentData {}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class ProjectIntoFutureOnCueSystem : SystemBase
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
#if HAVOK_PHYSICS_EXISTS
    private Havok.Physics.SimulationContext HavokSimulationContext;
#endif

    private JobHandle FinalJobHandle;
    private StepPhysicsWorld m_StepPhysicsWorld;
    private BuildPhysicsWorld m_BuildPhysicsWorld;

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

        SimulationContext = new SimulationContext();
#if HAVOK_PHYSICS_EXISTS
        HavokSimulationContext = new Havok.Physics.SimulationContext(Havok.Physics.HavokConfiguration.Default);
#endif
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

#if HAVOK_PHYSICS_EXISTS
    [BurstCompile]
    struct StepLocalWorldHavokJob : IJob
    {
        public SimulationStepInput StepInput;
        public Havok.Physics.SimulationContext SimulationContext;

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
            Havok.Physics.HavokSimulation.StepImmediate(StepInput, ref SimulationContext);
        }
    }
#endif

    protected override void OnCreate()
    {
        Positions = new NativeArray<float3>();
        LocalWorld = new PhysicsWorld();
        FinalJobHandle = new JobHandle();

        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    protected override void OnDestroy()
    {
        if (Positions.IsCreated) Positions.Dispose();
        if (LocalWorld.NumBodies != 0) LocalWorld.Dispose();

        SimulationContext.Dispose();
#if HAVOK_PHYSICS_EXISTS
        HavokSimulationContext.Dispose();
#endif
    }

    protected override void OnUpdate()
    {
        bool bUpdate = true;
        bUpdate &= (IsInitialized && NeedsUpdate);
        bUpdate &= !WhiteBallVelocity.Equals(float3.zero);
        if (!bUpdate)
            return;

        var jobHandle = Dependency;

        // Make PhysicsWorld safe to read
        FinalJobHandle = JobHandle.CombineDependencies(FinalJobHandle, m_StepPhysicsWorld.GetOutputDependency());

        // Complete the local simulation trails from the previous step.
        FinalJobHandle.Complete();

        ref var world = ref m_BuildPhysicsWorld.PhysicsWorld;
        CheckEntityPool(world.NumDynamicBodies);

        // Clear the trails ready for a new simulation prediction
        jobHandle = new ResetPositionsJob { Positions = Positions }.Schedule(jobHandle);

        // If a local world was previously cloned get rid of it and make a new one.
        if (LocalWorld.NumBodies > 0)
        {
            LocalWorld.Dispose();
        }
        LocalWorld = world.Clone();

        float timeStep = Time.DeltaTime;

        PhysicsStep stepComponent = PhysicsStep.Default;
        if (HasSingleton<PhysicsStep>())
        {
            stepComponent = GetSingleton<PhysicsStep>();
        }

        var stepInput = new SimulationStepInput
        {
            World = LocalWorld,
            TimeStep = timeStep,
            NumSolverIterations = stepComponent.SolverIterationCount,
            SolverStabilizationHeuristicSettings = stepComponent.SolverStabilizationHeuristicSettings,
            Gravity = stepComponent.Gravity,
            SynchronizeCollisionWorld = true
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
            if (stepComponent.SimulationType == SimulationType.UnityPhysics)
            {
                // TODO: look into a public version of SimulationContext.ScheduleReset
                // so that we can chain multiple StepLocalWorldJob instances.

                // Dispose and reallocate input velocity buffer, if dynamic body count has increased.
                // Dispose previous collision and trigger event streams and allocator new streams.
                SimulationContext.Reset(stepInput);

                new StepLocalWorldJob()
                {
                    StepInput = stepInput,
                    SimulationContext = SimulationContext,
                    StepIndex = i,
                    NumSteps = NumSteps,
                    TrailPositions = Positions
                }.Schedule().Complete();
            }
#if HAVOK_PHYSICS_EXISTS
            else
            {
                HavokSimulationContext.Reset(ref LocalWorld);
                new StepLocalWorldHavokJob()
                {
                    StepInput = stepInput,
                    SimulationContext = HavokSimulationContext,
                    StepIndex = i,
                    NumSteps = NumSteps,
                    TrailPositions = Positions
                }.Schedule().Complete();
            }
#endif
        }

        Dependency = jobHandle;
        var positions = Positions;
        var ghostScale = GhostScale;
        var numSteps = NumSteps;
        Entities
            .WithName("UpdateTrailEntityPositions")
            .WithBurst()
            .WithReadOnly(positions)
            .ForEach((Entity entity, int entityInQueryIndex, ref Translation t, ref Rotation r, ref NonUniformScale s, in ProjectIntoFutureTrail p) =>
            {
                var posT0 = positions[entityInQueryIndex];

                // Return if we are on the last step
                if ((entityInQueryIndex % numSteps) == (numSteps - 1))
                {
                    t.Value = posT0;
                    s.Value = ghostScale;
                    return;
                }

                // Get the next position
                var posT1 = positions[entityInQueryIndex + 1];

                // Return if we haven't moved
                var haveMovement = !posT0.Equals(posT1);
                if (!haveMovement)
                {
                    t.Value = posT0; // Comment this out to leave the trails after shot.
                    s.Value = ghostScale;
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
            }).Schedule();

        NeedsUpdate = false;

        FinalJobHandle = Dependency;
    }

    public void CullVelocity()
    {
        // Cull velocity on all the balls so that the simulation
        // will match the local prediction
        Entities
            .WithName("CullVelocities")
            .WithBurst()
            .ForEach((ref PhysicsVelocity pv) =>
            {
                pv.Linear = float3.zero;
                pv.Angular = float3.zero;
            }).Run();
    }
}

public class ProjectIntoFutureOnCueAuthoring : MonoBehaviour, IReceiveEntity
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
        System = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ProjectIntoFutureOnCueSystem>();
    }

    void Update()
    {
        if (!System.IsInitialized && !WhiteBallEntity.Equals(Entity.Null))
        {
            var physicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
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
            System.CullVelocity();
        }
    }

    public void OnButtonClick()
    {
        if (System.IsInitialized)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

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
