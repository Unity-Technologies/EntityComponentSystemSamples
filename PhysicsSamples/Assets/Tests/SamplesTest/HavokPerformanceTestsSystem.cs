using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Profiling;

namespace Unity.Physics.Tests.PerformanceTests
{
#if HAVOK_PHYSICS_EXISTS
    using ContactCounter = ProfilerCounterValue<int>;

#if ENABLE_PROFILER
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateContactsGroup))]
    [UpdateBefore(typeof(PhysicsCreateJacobiansGroup))]
    [CreateAfter(typeof(PhysicsInitializeGroup))]
    public partial struct HavokPerformanceTestsSystem : ISystem
    {
        private ContactCounter ContactCounter;
        public const string k_PhysicsContactCountName = "Havok Contact Count";

        [BurstCompile]
        internal readonly struct ContactCountJob : IJob
        {
            private readonly ContactCounter Contacts;
            private readonly int ContactCount;

            public ContactCountJob(ref ContactCounter contactCounter, int contactCount)
            {
                Contacts = contactCounter;
                ContactCount = contactCount;
            }

            public void Execute()
            {
                Contacts.Value  = ContactCount;
            }
        }

        void OnCreate(ref SystemState state)
        {
            ContactCounter = new ContactCounter(k_PhysicsContactCountName);
        }

        void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out PhysicsStep physicsStep))
            {
                return;
            }

            if (physicsStep.SimulationType == SimulationType.HavokPhysics)
            {
                var simSingleton = SystemAPI.GetSingletonRW<SimulationSingleton>().ValueRW;
                var sim = simSingleton.AsHavokSimulation();
                state.Dependency = new ContactCountJob(ref ContactCounter, sim.ContactsCount).Schedule(state.Dependency);
            }
            else
            {
                state.Enabled = false;
            }
        }
    }
#endif
#endif
}
