#if HAVOK_PHYSICS_EXISTS
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;

namespace Unity.Physics.Samples.Test
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
    partial class HavokPhysicsDeterminismTestSystem : SystemBase, IDeterminismTestSystem
    {
        private BuildPhysicsWorld m_BuildPhysicsWorld;
        private StepPhysicsWorld m_StepPhysicsWorld;
        private ExportPhysicsWorld m_ExportPhysicsWorld;
        private EnsureHavokSystem m_EnsureHavokSystem;
        private FixedStepSimulationSystemGroup m_FixedStepGroup;

        protected bool m_TestingFinished = false;
        protected bool m_RecordingBegan = false;

        public int SimulatedFramesInCurrentTest = 0;
        public const int k_TestDurationInFrames = 100;

        public void BeginTest()
        {
            SimulatedFramesInCurrentTest = 0;
            Enabled = true;
            m_FixedStepGroup.Enabled = true;
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
            m_FixedStepGroup = World.GetOrCreateSystem<FixedStepSimulationSystemGroup>();
        }

        protected void FinishTesting()
        {
            SimulatedFramesInCurrentTest = 0;
            m_FixedStepGroup.Enabled = false;
            m_ExportPhysicsWorld.Enabled = false;
            m_StepPhysicsWorld.Enabled = false;
            m_BuildPhysicsWorld.Enabled = false;
            Enabled = false;

            m_TestingFinished = true;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadOnly();

            // Read/write the display singleton to register as writing data
            // to make sure display systems don't interfere with the test
            if (HasSingleton<PhysicsDebugDisplayData>())
            {
                var data = GetSingleton<PhysicsDebugDisplayData>();
                SetSingleton(data);
            }
        }

        protected override void OnUpdate()
        {
            if (!m_RecordingBegan)
            {
                // > 1 because of default static body, logically should be > 0
                m_RecordingBegan = m_BuildPhysicsWorld.PhysicsWorld.NumBodies > 1;
            }
            else
            {
                SimulatedFramesInCurrentTest++;

                if (SimulatedFramesInCurrentTest == k_TestDurationInFrames)
                {
                    Dependency.Complete();
                    FinishTesting();
                }
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    partial class EnsureHavokSystem : SystemBase
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
#if (!UNITY_EDITOR && UNITY_PHYSICS_INCLUDE_SLOW_TESTS) || UNITY_PHYSICS_INCLUDE_END2END_TESTS
    [NUnit.Framework.TestFixture]
#endif
    class HavokPhysicsEndToEndDeterminismTest : UnityPhysicsEndToEndDeterminismTest
    {
        protected override IDeterminismTestSystem GetTestSystem() => DefaultWorld.GetExistingSystem<HavokPhysicsDeterminismTestSystem>();
    }
}
#endif
