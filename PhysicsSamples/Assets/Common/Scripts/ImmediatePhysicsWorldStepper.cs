using System;
using Unity.Entities;

namespace Unity.Physics.Systems
{
    /// <summary>
    /// Utility class for running physics simulation immediately on the main thread.
    /// Not suitable for a field in an ECS job!
    /// </summary>
    public class ImmediatePhysicsWorldStepper : IDisposable
    {
        // Simulation context
        public SimulationContext SimulationContext;
#if HAVOK_PHYSICS_EXISTS
        public Havok.Physics.SimulationContext HavokSimulationContext;
#endif

        /// <summary>
        /// Constructor. Do not use in system's OnCreate when simulation is Havok, as it won't pick up HavokConfiguration properly.
        /// </summary>
        public ImmediatePhysicsWorldStepper(SystemBase system, uint physicsWorldIndex)
        {
            SimulationContext = new SimulationContext();
#if HAVOK_PHYSICS_EXISTS
            Havok.Physics.HavokConfiguration config = system.HasSingleton<Havok.Physics.HavokConfiguration>() ?
                system.GetSingleton<Havok.Physics.HavokConfiguration>() : Havok.Physics.HavokConfiguration.Default;

            // We want to show different physics worlds in separate VDBs, so each one needs its own port
            config.VisualDebugger.Port += (int)physicsWorldIndex;
            HavokSimulationContext = new Havok.Physics.SimulationContext(config);
#endif
        }

        /// <summary>
        /// Steps the UnityPhysics simulation (on current thread)
        /// </summary>
        public static void StepUnityPhysicsSimulationImmediate(in SimulationStepInput stepInput, ref SimulationContext simulationContext)
        {
            Simulation.StepImmediate(stepInput, ref simulationContext);
        }

#if HAVOK_PHYSICS_EXISTS
        /// <summary>
        /// Steps the HavokPhysics simulation (on current thread)
        /// </summary>
        public static void StepHavokPhysicsSimulationImmediate(in SimulationStepInput stepInput, ref Havok.Physics.SimulationContext havokSimulationContext)
        {
            Havok.Physics.HavokSimulation.StepImmediate(stepInput, ref havokSimulationContext);
        }

#endif

        public void Dispose()
        {
            SimulationContext.Dispose();
#if HAVOK_PHYSICS_EXISTS
            HavokSimulationContext.Dispose();
#endif
        }

        /// <summary>
        /// Prepares simulation context and steps the simulation (on current thread)
        /// </summary>
        public void StepImmediate(SimulationType simType, ref PhysicsWorld physicsWorld, in SimulationStepInput stepInput)
        {
            if (simType == SimulationType.UnityPhysics)
            {
                SimulationContext.Reset(stepInput);
                Simulation.StepImmediate(stepInput, ref SimulationContext);
            }
#if HAVOK_PHYSICS_EXISTS
            else
            {
                HavokSimulationContext.Reset(ref physicsWorld);
                Havok.Physics.HavokSimulation.StepImmediate(stepInput, ref HavokSimulationContext);
            }
#endif
        }
    }
}
