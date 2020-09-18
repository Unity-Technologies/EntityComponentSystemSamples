#if HAVOK_PHYSICS_EXISTS
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Jobs;

namespace Unity.Physics.Samples.Test
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
    class HavokPhysicsDeterminismTestSystem : SystemBase, IDeterminismTestSystem
    {
        private BuildPhysicsWorld m_BuildPhysicsWorld;
        private StepPhysicsWorld m_StepPhysicsWorld;
        private ExportPhysicsWorld m_ExportPhysicsWorld;
        private EnsureHavokSystem m_EnsureHavokSystem;
        protected bool m_TestingFinished = false;

        public int SimulatedFramesInCurrentTest = 0;
        public const int k_TestDurationInFrames = 100;

        public void BeginTest()
        {
            SimulatedFramesInCurrentTest = 0;
            Enabled = true;
            m_ExportPhysicsWorld.Enabled = true;
            m_StepPhysicsWorld.Enabled = true;
            m_BuildPhysicsWorld.Enabled = true;
            m_EnsureHavokSystem.Enabled = true;

            m_TestingFinished = false;
        }

        public bool TestingFinished() => m_TestingFinished;

        protected override void OnCreate()
        {
            Enabled = false;

            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_ExportPhysicsWorld = World.GetOrCreateSystem<ExportPhysicsWorld>();
            m_EnsureHavokSystem = World.GetOrCreateSystem<EnsureHavokSystem>();
        }

        protected void FinishTesting()
        {
            SimulatedFramesInCurrentTest = 0;
            m_ExportPhysicsWorld.Enabled = false;
            m_StepPhysicsWorld.Enabled = false;
            m_BuildPhysicsWorld.Enabled = false;
            Enabled = false;

            m_TestingFinished = true;
        }

        protected override void OnUpdate()
        {
            SimulatedFramesInCurrentTest++;
            var handle = JobHandle.CombineDependencies(Dependency, m_ExportPhysicsWorld.GetOutputDependency());

            if (SimulatedFramesInCurrentTest == k_TestDurationInFrames)
            {
                handle.Complete();
                FinishTesting();
            }

            Dependency = handle;
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    class EnsureHavokSystem : SystemBase
    {
        protected override void OnCreate()
        {
            Enabled = false;
        }

        public static void EnsureHavok(SystemBase system)
        {
            if (system.HasSingleton<PhysicsStep>())
            {
                var component = system.GetSingleton<PhysicsStep>();
                if (component.SimulationType != SimulationType.HavokPhysics)
                {
                    component.SimulationType = SimulationType.HavokPhysics;
                    system.SetSingleton(component);
                }
                system.Enabled = false;
            }
        }

        protected override void OnUpdate()
        {
            EnsureHavok(this);
        }
    }

    // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR && UNITY_PHYSICS_INCLUDE_SLOW_TESTS
    [NUnit.Framework.TestFixture]
#endif
    class HavokPhysicsEndToEndDeterminismTest : UnityPhysicsEndToEndDeterminismTest
    {
        protected override IDeterminismTestSystem GetTestSystem() => DefaultWorld.GetExistingSystem<HavokPhysicsDeterminismTestSystem>();
    }
}
#endif
