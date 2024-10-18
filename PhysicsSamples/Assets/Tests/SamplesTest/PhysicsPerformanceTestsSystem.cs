using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Profiling;

namespace Unity.Physics.Tests.PerformanceTests
{
    using ContactCounter = ProfilerCounterValue<int>;

#if ENABLE_PROFILER
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateContactsGroup))]
    [UpdateBefore(typeof(PhysicsCreateJacobiansGroup))]
    [CreateAfter(typeof(PhysicsInitializeGroup))]
    public partial struct PhysicsPerformanceTestsSystem : ISystem
    {
        private ContactCounter ContactCounter;
        public const string k_PhysicsContactCountName = "Unity Physics Contact Count";

        [BurstCompile]
        private readonly struct ContactCountJob : IJob
        {
            [WriteOnly] private readonly ContactCounter ContactCounter;
            [ReadOnly] private readonly NativeStream Contacts;

            public ContactCountJob(ref ContactCounter contactCounter, NativeStream contacts)
            {
                ContactCounter = contactCounter;
                Contacts = contacts;
            }

            public void Execute()
            {
                if (Contacts.IsCreated)
                {
                    ContactCounter.Value = Contacts.Count();
                }
            }
        }

        public void OnCreate(ref SystemState state)
        {
            ContactCounter = new ContactCounter(k_PhysicsContactCountName);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out PhysicsStep physicsStep))
            {
                return;
            }

            if (physicsStep.SimulationType == SimulationType.UnityPhysics)
            {
                var simSingleton = SystemAPI.GetSingletonRW<SimulationSingleton>().ValueRW;
                var sim = simSingleton.AsSimulation();
                if (sim.Contacts.IsCreated)
                {
                    state.Dependency = new ContactCountJob(ref ContactCounter, sim.Contacts).Schedule(state.Dependency);
                }
            }
            else
            {
                state.Enabled = false;
            }
        }
    }
#endif
}
