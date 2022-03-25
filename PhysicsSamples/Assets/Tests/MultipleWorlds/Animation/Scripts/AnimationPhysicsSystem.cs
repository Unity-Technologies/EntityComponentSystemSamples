using Unity.Entities;
using Unity.Physics.Systems;

namespace Unity.Physics.Tests
{
    // A system which performs the whole physics pipeline on animated bodies
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(DriveAnimationBodySystem))]
    public partial class AnimationPhysicsSystem : SystemBase
    {
        private PhysicsWorldData PhysicsData;

        private PhysicsWorldIndex WorldFilter;

        private ImmediatePhysicsWorldStepper m_Stepper;

        protected override void OnCreate()
        {
            base.OnCreate();

            WorldFilter = new PhysicsWorldIndex(1);
            PhysicsData = new PhysicsWorldData(EntityManager, WorldFilter);
        }

        protected override void OnDestroy()
        {
            if (m_Stepper != null)
            {
                m_Stepper.Dispose();
            }

            PhysicsData.Dispose();

            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (m_Stepper == null)
            {
                // Initialize immediate stepper here, since it can't be done in OnCreate.
                m_Stepper = new ImmediatePhysicsWorldStepper(this, WorldFilter.Value);
            }
        }

        protected override void OnUpdate()
        {
            // Make sure dependencies are complete, we'll run everything immediately
            Dependency.Complete();

            float timeStep = Time.DeltaTime;

            // Tweak if you want a different simulation for this world
            PhysicsStep stepComponent = PhysicsStep.Default;
            if (HasSingleton<PhysicsStep>())
            {
                stepComponent = GetSingleton<PhysicsStep>();
            }

            // Build PhysicsWorld immediately
            PhysicsWorldBuilder.BuildPhysicsWorldImmediate(this, ref PhysicsData, timeStep, stepComponent.Gravity, LastSystemVersion);

            // Early out if world is static
            if (PhysicsData.PhysicsWorld.NumDynamicBodies == 0) return;

            // Run simulation on main thread
            m_Stepper.StepImmediate(stepComponent.SimulationType, ref PhysicsData.PhysicsWorld,
                new SimulationStepInput()
                {
                    World = PhysicsData.PhysicsWorld,
                    TimeStep = timeStep,
                    Gravity = stepComponent.Gravity,
                    SynchronizeCollisionWorld = false,
                    NumSolverIterations = 4,
                    SolverStabilizationHeuristicSettings = Solver.StabilizationHeuristicSettings.Default,
                    HaveStaticBodiesChanged = PhysicsData.HaveStaticBodiesChanged
                });

            // Export physics world only (don't copy CollisionWorld)
            PhysicsWorldExporter.ExportPhysicsWorldImmediate(this, in PhysicsData.PhysicsWorld, PhysicsData.DynamicEntityGroup);
        }
    }
}
