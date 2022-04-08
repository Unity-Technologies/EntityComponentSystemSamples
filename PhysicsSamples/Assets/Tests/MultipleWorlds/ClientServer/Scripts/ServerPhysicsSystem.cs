using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;

namespace Unity.Physics.Tests
{
    // Represents runtime data of server physics world - used only to declare reading/writing the data.
    internal struct ServerPhysicsSystemRuntimeData : IComponentData
    {
        byte DummyValue;
    }

    /// <summary>
    /// System which performs server physics simulation (builds, steps and exports the server PhysicsWorld)
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(DriveGhostBodySystem))]
    [AlwaysUpdateSystem]
    public partial class ServerPhysicsSystem : SystemBase
    {
        public PhysicsWorldData PhysicsData;
        public PhysicsWorldIndex WorldFilter;

        private PhysicsWorldStepper m_Stepper;
        private JobHandle m_InputDependencyToComplete;

        // Entity query that is used for preventing other systems from changing colliders through PhysicsCollider component
        // (both RigidBody from PhysicsWorld and PhysicsCollider component point to the same collider blob asset).
        private EntityQuery m_PhysicsColliderQuery;

        // Enqueue a callback to run during scheduling of the next simulation step
        public void EnqueueCallback(SimulationCallbacks.Phase phase, SimulationCallbacks.Callback callback, JobHandle dependency = default) => m_Stepper.EnqueueCallback(phase, callback, dependency);

        public void AddInputDependencyToComplete(JobHandle dependencyToComplete)
        {
            m_InputDependencyToComplete = JobHandle.CombineDependencies(m_InputDependencyToComplete, dependencyToComplete);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            WorldFilter = new PhysicsWorldIndex(3);
            PhysicsData = new PhysicsWorldData(EntityManager, WorldFilter);


            // Declare read/write access to Server entities' PhysicsColliders
            // Theoretically, colliders we need here (in Server world) could be changed from PhysicsCollider component on any Entity (since colliders are usually shared between entities),
            // but we're assuming that server and client-only bodies don't share colliders and that ghost Main world entities will never be changed (they're just server replicas).
            m_PhysicsColliderQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(PhysicsCollider),
                    typeof(PhysicsWorldIndex)
                }
            });
            m_PhysicsColliderQuery.SetSharedComponentFilter(WorldFilter);

            // Make sure ServerPhysicsSystemRuntimeData is registered as singleton, so we can use it to control client physics world access
            var runtimeDataEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(runtimeDataEntity, new ServerPhysicsSystemRuntimeData());

            m_Stepper = new PhysicsWorldStepper();
        }

        protected override void OnDestroy()
        {
            m_Stepper.Dispose();
            PhysicsData.Dispose();

            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            // Declare read/write access to server physics world
            this.RegisterPhysicsRuntimeSystemReadWrite<ServerPhysicsSystemRuntimeData>();
        }

        protected override void OnUpdate()
        {
            // Make sure last frame's physics jobs are complete before any new ones start
            m_InputDependencyToComplete.Complete();

            float timeStep = Time.DeltaTime;

            // Tweak if you want a different simulation for this world
            PhysicsStep stepComponent = PhysicsStep.Default;
            if (HasSingleton<PhysicsStep>())
            {
                stepComponent = GetSingleton<PhysicsStep>();
            }

            #region Build World

            Dependency = PhysicsWorldBuilder.SchedulePhysicsWorldBuild(this, ref PhysicsData,
                Dependency, timeStep, stepComponent.MultiThreaded > 0, stepComponent.Gravity, LastSystemVersion);

            #endregion

            #region Step World

            // Early-out to prevent simulation context creation in stepper, generally not required
            if (PhysicsData.PhysicsWorld.NumBodies <= 1)
            {
                return;
            }

            var stepInput = new SimulationStepInput()
            {
                World = PhysicsData.PhysicsWorld,
                TimeStep = timeStep,
                Gravity = stepComponent.Gravity,
                SynchronizeCollisionWorld = false,
                NumSolverIterations = stepComponent.SolverIterationCount,
                SolverStabilizationHeuristicSettings = stepComponent.SolverStabilizationHeuristicSettings,
                HaveStaticBodiesChanged = PhysicsData.HaveStaticBodiesChanged,
            };

            m_Stepper.ScheduleSimulationStepJobs(stepComponent.SimulationType, WorldFilter.Value, stepInput, Dependency, stepComponent.MultiThreaded > 0);

            // Include the final simulation handle
            // (Not FinalJobHandle since other systems shouldn't need to depend on the dispose jobs)
            Dependency = JobHandle.CombineDependencies(Dependency, m_Stepper.FinalSimulationJobHandle);

            #endregion

            #region Export World

            Dependency = PhysicsWorldExporter.SchedulePhysicsWorldExport(this, in PhysicsData.PhysicsWorld, Dependency, PhysicsData.DynamicEntityGroup);

            #endregion

            // Just to make sure current server step jobs are complete before next step starts
            m_InputDependencyToComplete = Dependency;
        }
    }
}
