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

public class ProjectIntoFutureOnCueData : IComponentData
{
    public bool NeedsUpdate;
    public int NumSteps;
    public float3 WhiteBallVelocity;
    public Entity WhiteBallEntity;
    public RenderMeshArray GhostMaterial;
    public float GhostScale;
    public NativeArray<float3> Positions;
    public PhysicsWorld LocalWorld;
    public ImmediatePhysicsWorldStepper ImmediatePhysicsStepper;

    public bool IsInitialized => !WhiteBallEntity.Equals(Entity.Null);

    public void Initialize(EntityManager manager, Entity whiteBallEntity, int numSteps, Mesh referenceMesh, Material referenceMaterial, in PhysicsWorld physicsWorld)
    {
        WhiteBallEntity = whiteBallEntity;
        NumSteps = numSteps;
        GhostMaterial = new RenderMeshArray(new[] { referenceMaterial }, new[] { referenceMesh });

        if (!ImmediatePhysicsStepper.Created)
        {
            ImmediatePhysicsStepper = ImmediatePhysicsWorldStepper.Create();
        }

        CheckEntityPool(manager, physicsWorld.NumDynamicBodies);

        NeedsUpdate = true;
    }

    public void CheckEntityPool(EntityManager manager, int numDynamicBodies)
    {
        int totalNumberOfEntities = NumSteps * numDynamicBodies;
        int diff = totalNumberOfEntities - Positions.Length;

        if (diff <= 0)
        {
            return;
        }

        if (Positions.IsCreated) Positions.Dispose();
        Positions = new NativeArray<float3>(totalNumberOfEntities, Allocator.Persistent);

        for (int i = 0; i < diff; i++)
        {
            var ghost = manager.Instantiate(WhiteBallEntity);

            manager.RemoveComponent<PhysicsCollider>(ghost);
            manager.RemoveComponent<PhysicsVelocity>(ghost);

            manager.AddComponentData(ghost, new ProjectIntoFutureTrail());
            manager.AddSharedComponentManaged(ghost, GhostMaterial);

#if !ENABLE_TRANSFORM_V1
            var scale = new PostTransformScale { Value = float3x3.Scale(GhostScale) };
#else
            var scale = new NonUniformScale { Value = GhostScale};
#endif
            manager.AddComponentData(ghost, scale);
        }

        NeedsUpdate = true;
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct ProjectIntoFutureOnCueSystem : ISystem
{
    [BurstCompile]
    [WithAll(typeof(ProjectIntoFutureTrail))]
    private partial struct IJobEntity_ProjectIntoFutureTrail : IJobEntity
    {
        public NativeArray<float3> Positions;
        public float GhostScale;
        public int NumSteps;

#if !ENABLE_TRANSFORM_V1
        public void Execute([EntityIndexInQuery] int entityInQueryIndex, Entity entity, ref LocalTransform localTransform, ref PostTransformScale postTransformScale)
#else
        public void Execute([EntityIndexInQuery] int entityInQueryIndex, Entity entity, ref Translation translation, ref Rotation rotation, ref NonUniformScale scale)
#endif
        {
            var posT0 = Positions[entityInQueryIndex];

            // Return if we are on the last step
            if ((entityInQueryIndex % NumSteps) == (NumSteps - 1))
            {
#if !ENABLE_TRANSFORM_V1
                localTransform.Position = posT0;
                postTransformScale.Value = float3x3.Scale(GhostScale);
#else
                translation.Value = posT0;
                scale.Value = GhostScale;
#endif
                return;
            }

            // Get the next position
            var posT1 = Positions[entityInQueryIndex + 1];

            // Return if we haven't moved
            var haveMovement = !posT0.Equals(posT1);
            if (!haveMovement)
            {
#if !ENABLE_TRANSFORM_V1
                localTransform.Position = posT0; // Comment this out to leave the trails after shot.
                postTransformScale.Value = float3x3.Scale(GhostScale);
#else
                translation.Value = posT0; // Comment this out to leave the trails after shot.
                scale.Value = GhostScale;
#endif
                return;
            }

            // Position the ghost ball half way between T0 and T1
#if !ENABLE_TRANSFORM_V1
            localTransform.Position = math.lerp(posT0, posT1, 0.5f);
#else
            translation.Value = math.lerp(posT0, posT1, 0.5f);
#endif

            // Orientation the ball along the direction between T0 and T1
            // and stretch the ball between those 2 positions.
            var forward = posT1 - posT0;
            var scaleValue = math.length(forward);
            var rotationValue = quaternion.LookRotationSafe(forward, new float3(0, 1, 0));

#if !ENABLE_TRANSFORM_V1
            localTransform.Rotation = rotationValue;
            postTransformScale.Value.c2.z = scaleValue;
#else
            rotation.Value = rotationValue;
            scale.Value = scaleValue;
#endif
        }
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
            ImmediatePhysicsWorldStepper.StepUnityPhysicsSimulationImmediate(StepInput, ref SimulationContext);
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
            ImmediatePhysicsWorldStepper.StepHavokPhysicsSimulationImmediate(StepInput, ref SimulationContext);
        }
    }
#endif

    public void OnCreate(ref SystemState state)
    {
        state.EntityManager.AddComponentObject(state.SystemHandle, new ProjectIntoFutureOnCueData
        {
            NeedsUpdate = true,
            NumSteps = 0,
            WhiteBallVelocity = 0f,
            WhiteBallEntity = Entity.Null,
            GhostScale = 0.01f,
            LocalWorld = new PhysicsWorld(),
            Positions = new NativeArray<float3>(),
            GhostMaterial = default,
        });
    }

    public void OnDestroy(ref SystemState state)
    {
        state.CompleteDependency();

        var projectIntoFutureOnCueData = state.EntityManager.GetComponentObject<ProjectIntoFutureOnCueData>(state.SystemHandle);
        if (projectIntoFutureOnCueData.Positions.IsCreated) projectIntoFutureOnCueData.Positions.Dispose();
        if (projectIntoFutureOnCueData.LocalWorld.NumBodies != 0) projectIntoFutureOnCueData.LocalWorld.Dispose();
        if (projectIntoFutureOnCueData.ImmediatePhysicsStepper.Created) projectIntoFutureOnCueData.ImmediatePhysicsStepper.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Make PhysicsWorld safe to read
        // Complete the local simulation trails from the previous step.
        state.Dependency.Complete();

        var projectIntoFutureOnCueData = state.EntityManager.GetComponentObject<ProjectIntoFutureOnCueData>(state.SystemHandle);
        bool bUpdate = true;
        bUpdate &= (projectIntoFutureOnCueData.IsInitialized && projectIntoFutureOnCueData.NeedsUpdate);
        bUpdate &= !projectIntoFutureOnCueData.WhiteBallVelocity.Equals(float3.zero);
        if (!bUpdate)
            return;

        var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        projectIntoFutureOnCueData.CheckEntityPool(state.EntityManager, world.NumDynamicBodies);

        // Clear the trails ready for a new simulation prediction
        new ResetPositionsJob { Positions = projectIntoFutureOnCueData.Positions}.Run();

        // If a local world was previously cloned get rid of it and make a new one.
        if (projectIntoFutureOnCueData.LocalWorld.NumBodies > 0)
        {
            projectIntoFutureOnCueData.LocalWorld.Dispose();
        }
        projectIntoFutureOnCueData.LocalWorld = world.Clone();

        float timeStep = SystemAPI.Time.DeltaTime;

        PhysicsStep stepComponent = PhysicsStep.Default;
        if (SystemAPI.HasSingleton<PhysicsStep>())
        {
            stepComponent = SystemAPI.GetSingleton<PhysicsStep>();
        }

        var bpwData = SystemAPI.GetSingleton<BuildPhysicsWorldData>();
        var stepInput = new SimulationStepInput
        {
            World = projectIntoFutureOnCueData.LocalWorld,
            TimeStep = timeStep,
            NumSolverIterations = stepComponent.SolverIterationCount,
            SolverStabilizationHeuristicSettings = stepComponent.SolverStabilizationHeuristicSettings,
            Gravity = stepComponent.Gravity,
            SynchronizeCollisionWorld = true,
            HaveStaticBodiesChanged = bpwData.HaveStaticBodiesChanged
        };

        // Assign the requested cue ball velocity to the local simulation
        projectIntoFutureOnCueData.LocalWorld.SetLinearVelocity(projectIntoFutureOnCueData.LocalWorld.GetRigidBodyIndex(projectIntoFutureOnCueData.WhiteBallEntity), projectIntoFutureOnCueData.WhiteBallVelocity);

        // Sync the CollisionWorld before the initial step.
        // As stepInput.SynchronizeCollisionWorld is true the simulation will
        // automatically sync the CollisionWorld on subsequent steps.
        // This is only needed as we have modified the cue ball velocity.
        projectIntoFutureOnCueData.LocalWorld.CollisionWorld.ScheduleUpdateDynamicTree(
            ref projectIntoFutureOnCueData.LocalWorld, stepInput.TimeStep, stepInput.Gravity, default, false)
            .Complete();

        // Step the local world
        for (int i = 0; i < projectIntoFutureOnCueData.NumSteps; i++)
        {
            if (stepComponent.SimulationType == SimulationType.UnityPhysics)
            {
                // TODO: look into a public version of SimulationContext.ScheduleReset
                // so that we can chain multiple StepLocalWorldJob instances.

                // Dispose and reallocate input velocity buffer, if dynamic body count has increased.
                // Dispose previous collision and trigger event streams and allocator new streams.
                projectIntoFutureOnCueData.ImmediatePhysicsStepper.SimulationContext.Reset(stepInput);

                new StepLocalWorldJob()
                {
                    StepInput = stepInput,
                    SimulationContext = projectIntoFutureOnCueData.ImmediatePhysicsStepper.SimulationContext,
                    StepIndex = i,
                    NumSteps = projectIntoFutureOnCueData.NumSteps,
                    TrailPositions = projectIntoFutureOnCueData.Positions
                }.Run();
            }
#if HAVOK_PHYSICS_EXISTS
            else
            {
                projectIntoFutureOnCueData.ImmediatePhysicsStepper.HavokSimulationContext.Reset(ref projectIntoFutureOnCueData.LocalWorld);
                new StepLocalWorldHavokJob()
                {
                    StepInput = stepInput,
                    SimulationContext = projectIntoFutureOnCueData.ImmediatePhysicsStepper.HavokSimulationContext,
                    StepIndex = i,
                    NumSteps = projectIntoFutureOnCueData.NumSteps,
                    TrailPositions = projectIntoFutureOnCueData.Positions
                }.Run();
            }
#endif
        }

        state.Dependency = new IJobEntity_ProjectIntoFutureTrail
        {
            Positions = projectIntoFutureOnCueData.Positions,
            GhostScale = projectIntoFutureOnCueData.GhostScale,
            NumSteps = projectIntoFutureOnCueData.NumSteps
        }.Schedule(state.Dependency);

        projectIntoFutureOnCueData.NeedsUpdate = false;
    }
}

public class ProjectIntoFutureOnCueAuthoring : MonoBehaviour
{
    public Mesh ReferenceMesh;
    public Material ReferenceMaterial;
    public Slider RotateSlider;
    public Slider StrengthSlider;
    public int NumSteps = 25;

    private Entity WhiteBallEntity = Entity.Null;
    private EntityQuery WhiteBallQuery;
    private EntityQuery ProjectIntoFutureOnCueDataQuery;
    private EntityQuery m_PhysicsVelocityQuery;

    private bool m_DidStart = false;

    private float3 GetVelocityFromSliders()
    {
        float angle = RotateSlider.value - 90;
        float strength = StrengthSlider.value;
        float3 velocity = strength * math.forward(quaternion.AxisAngle(math.up(), math.radians(angle)));

        return velocity;
    }

    void Start()
    {
        m_DidStart = true;

        WhiteBallQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(WhiteBall));
        ProjectIntoFutureOnCueDataQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(ProjectIntoFutureOnCueData));
        m_PhysicsVelocityQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(PhysicsVelocity));
    }

    void OnDestroy()
    {
        if (World.DefaultGameObjectInjectionWorld?.IsCreated == true)
        {
            if (World.DefaultGameObjectInjectionWorld.EntityManager.IsQueryValid(WhiteBallQuery))
            {
                WhiteBallQuery.Dispose();
            }
            if (World.DefaultGameObjectInjectionWorld.EntityManager.IsQueryValid(ProjectIntoFutureOnCueDataQuery))
            {
                ProjectIntoFutureOnCueDataQuery.Dispose();
            }
            if (World.DefaultGameObjectInjectionWorld.EntityManager.IsQueryValid(m_PhysicsVelocityQuery))
            {
                m_PhysicsVelocityQuery.Dispose();
            }
        }
    }

    void Update()
    {
        if (WhiteBallEntity.Equals(Entity.Null) &&
            World.DefaultGameObjectInjectionWorld.EntityManager.IsQueryValid(WhiteBallQuery) &&
            !WhiteBallQuery.IsEmpty)
        {
            WhiteBallEntity = WhiteBallQuery.GetSingletonEntity();
        }

        ProjectIntoFutureOnCueData data = GetData();
        if (data != null)
        {
            if (!data.IsInitialized && !WhiteBallEntity.Equals(Entity.Null))
            {
                EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<PhysicsWorldSingleton>();
                EntityQuery singletonQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(builder);
                PhysicsWorld physicsWorld = singletonQuery.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
                if (physicsWorld.NumDynamicBodies > 0)
                {
                    data.Initialize(World.DefaultGameObjectInjectionWorld.EntityManager, WhiteBallEntity, NumSteps, ReferenceMesh, ReferenceMaterial, in physicsWorld);
                    data.WhiteBallVelocity = GetVelocityFromSliders();
                    data.NeedsUpdate = true;
                }
                singletonQuery.Dispose();
            }
        }
    }

    public ProjectIntoFutureOnCueData GetData()
    {
        SystemHandle handle = World.DefaultGameObjectInjectionWorld.GetExistingSystem(typeof(ProjectIntoFutureOnCueSystem));
        return World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentObject<ProjectIntoFutureOnCueData>(handle);
    }

    public void CullVelocity()
    {
        int entityCount = m_PhysicsVelocityQuery.CalculateEntityCount();
        NativeArray<PhysicsVelocity> velocities = new NativeArray<PhysicsVelocity>(entityCount, Allocator.Temp);
        for (int i = 0; i < entityCount; i++)
        {
            velocities[i] = new PhysicsVelocity
            {
                Angular = float3.zero,
                Linear = float3.zero
            };
        }
        m_PhysicsVelocityQuery.CopyFromComponentDataArray(velocities);
    }

    public void OnSliderValueChanged()
    {
        if (m_DidStart)
        {
            ProjectIntoFutureOnCueData data = GetData();
            if (data != null && data.IsInitialized)
            {
                data.WhiteBallVelocity = GetVelocityFromSliders();
                data.NeedsUpdate = true;
                CullVelocity();
            }
        }
    }

    public void OnButtonClick()
    {
        if (m_DidStart)
        {
            ProjectIntoFutureOnCueData data = GetData();

            if (data != null)
            {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                // assign the required velocity to the white ball in the main simulation
                var velocity = entityManager.GetComponentData<PhysicsVelocity>(WhiteBallEntity);
                velocity.Linear = GetVelocityFromSliders();
                entityManager.SetComponentData(WhiteBallEntity, velocity);

                data.WhiteBallVelocity = float3.zero;
                data.NeedsUpdate = true;
            }
        }
    }
}
