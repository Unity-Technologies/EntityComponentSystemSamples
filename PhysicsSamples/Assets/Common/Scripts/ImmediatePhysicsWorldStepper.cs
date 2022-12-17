using System;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Physics.Systems
{
    /// <summary>
    /// Utility class for running physics simulation immediately on the main thread.
    /// Not suitable for a field in an ECS job!
    /// </summary>
    public struct ImmediatePhysicsWorldStepper : IDisposable
    {
        // Simulation context
        public SimulationContext SimulationContext;
#if HAVOK_PHYSICS_EXISTS
        public Havok.Physics.SimulationContext HavokSimulationContext;
#endif

        public bool Created;

        /// <summary>
        /// Create method. Use this instead of constructor.
        /// </summary>
        public static ImmediatePhysicsWorldStepper Create()
        {
            ImmediatePhysicsWorldStepper instance = new ImmediatePhysicsWorldStepper();
            instance.Created = true;
            instance.SimulationContext = new SimulationContext();

#if HAVOK_PHYSICS_EXISTS
            instance.HavokSimulationContext = new Havok.Physics.SimulationContext(Havok.Physics.HavokConfiguration.Default);
#endif

            return instance;
        }

#if HAVOK_PHYSICS_EXISTS
        /// <summary>
        /// Create method. Provides an option to pass in HavokConfiguration (for example, VDB options).
        /// </summary>
        /// <param name="havokConfiguration"></param>
        public static ImmediatePhysicsWorldStepper Create(Havok.Physics.HavokConfiguration havokConfiguration)
        {
            ImmediatePhysicsWorldStepper instance = new ImmediatePhysicsWorldStepper();
            instance.Created = true;
            instance.SimulationContext = new SimulationContext();
            instance.HavokSimulationContext = new Havok.Physics.SimulationContext(havokConfiguration);
            return instance;
        }

#endif

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
            Created = false;
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
