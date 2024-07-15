using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Unity.NetCode
{
    /// <summary>
    /// Run the physics step using the Immediate mode. this is usually way faster than running jobs when the number of
    /// entities is relatively small.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation|WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [BurstCompile]
    public partial struct ImmediatePhysicsStep : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.Enabled = false;
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pw = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>();
            state.CompleteDependency();
            var stepTime = SystemAPI.Time.DeltaTime;
            var simulation = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
            if (!SystemAPI.TryGetSingleton<PhysicsStep>(out var physicsStep))
                physicsStep = PhysicsStep.Default;
            ref var buildPhysicData = ref state.EntityManager.GetComponentDataRW<BuildPhysicsWorldData>(
                state.WorldUnmanaged.GetExistingUnmanagedSystem<BuildPhysicsWorld>()).ValueRW;
            var simulationStepInput = new SimulationStepInput
            {
                World = pw.ValueRW.PhysicsWorld,
                TimeStep = stepTime,
                Gravity = physicsStep.Gravity,
                NumSolverIterations = physicsStep.SolverIterationCount,
                SynchronizeCollisionWorld = physicsStep.SynchronizeCollisionWorld != 0,
                SolverStabilizationHeuristicSettings = physicsStep.SolverStabilizationHeuristicSettings,
                HaveStaticBodiesChanged = buildPhysicData.PhysicsData.HaveStaticBodiesChanged
            };
            //This can also be executed on a job worker thread technically speaking.
            simulation.ResetSimulationContext(simulationStepInput);
            simulation.Step(simulationStepInput);
        }
    }
}
