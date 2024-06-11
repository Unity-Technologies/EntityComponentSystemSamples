using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using static Unity.Physics.Systems.PhysicsWorldExporter;

namespace Unity.Physics.Tests
{
    // A system which performs the whole physics pipeline on animated bodies
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(DriveAnimationBodySystem))]
    public partial struct AnimationPhysicsSystem : ISystem, ISystemStartStop
    {
        private PhysicsWorldData PhysicsData;
        private PhysicsWorldIndex WorldFilter;
        private ImmediatePhysicsWorldStepper m_Stepper;
        private ExportPhysicsWorldTypeHandles m_ExportPhysicsWorldTypeHandles;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DriveAnimationBodyData>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            WorldFilter = new PhysicsWorldIndex(1);
            PhysicsData = new PhysicsWorldData(ref state, WorldFilter);
            m_ExportPhysicsWorldTypeHandles = new ExportPhysicsWorldTypeHandles(ref state);
            m_Stepper = ImmediatePhysicsWorldStepper.Create();
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            if (m_Stepper.Created == true)
            {
                m_Stepper.Dispose();
            }

            PhysicsData.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Make sure dependencies are complete, we'll run everything immediately
            state.CompleteDependency();

            float timeStep = SystemAPI.Time.DeltaTime;

            // Tweak if you want a different simulation for this world
            PhysicsStep stepComponent = PhysicsStep.Default;
            if (SystemAPI.HasSingleton<PhysicsStep>())
            {
                stepComponent = SystemAPI.GetSingleton<PhysicsStep>();
            }

            // Build PhysicsWorld immediately
            PhysicsWorldBuilder.BuildPhysicsWorldImmediate(ref state, ref PhysicsData, timeStep, stepComponent.Gravity, state.LastSystemVersion);

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
            ExportPhysicsWorldImmediate(ref state, ref m_ExportPhysicsWorldTypeHandles, in PhysicsData.PhysicsWorld, PhysicsData.DynamicEntityGroup);
        }
    }
}
