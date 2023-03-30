#if HAVOK_PHYSICS_EXISTS && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;

namespace Unity.Physics.Tests
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    partial class HavokPhysicsDeterminismTestSystem : SystemBase, IDeterminismTestSystem
    {
        protected bool m_TestingFinished = false;
        protected bool m_RecordingBegan = false;

        public int SimulatedFramesInCurrentTest = 0;
        public const int k_TestDurationInFrames = 100;

        public void BeginTest()
        {
            SimulatedFramesInCurrentTest = 0;
            Enabled = true;
            var component = SystemAPI.GetSingleton<PhysicsStep>();
            if (component.SimulationType != SimulationType.HavokPhysics)
            {
                component.SimulationType = SimulationType.HavokPhysics;
                SystemAPI.SetSingleton(component);
            }

            World.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>().Enabled = true;
            m_TestingFinished = false;
        }

        public bool TestingFinished() => m_TestingFinished;

        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected void FinishTesting()
        {
            SimulatedFramesInCurrentTest = 0;
            World.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>().Enabled = true;
            Enabled = false;

            m_TestingFinished = true;
        }

        protected override void OnStartRunning()
        {
            // Read/write the display singleton to register as writing data
            // to make sure display systems don't interfere with the test
            if (SystemAPI.HasSingleton<PhysicsDebugDisplayData>())
            {
                var data = SystemAPI.GetSingleton<PhysicsDebugDisplayData>();
                SystemAPI.SetSingleton(data);
            }
        }

        protected override void OnUpdate()
        {
            if (!m_RecordingBegan)
            {
                // > 1 because of default static body, logically should be > 0
                m_RecordingBegan = SystemAPI.GetSingleton<PhysicsWorldSingleton>().NumBodies > 1;
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

    // Only works in standalone build, since it needs synchronous Burst compilation.
#if (!UNITY_EDITOR && UNITY_PHYSICS_INCLUDE_SLOW_TESTS) || UNITY_PHYSICS_INCLUDE_END2END_TESTS
    [NUnit.Framework.TestFixture]
#endif
    class HavokPhysicsEndToEndDeterminismTest : UnityPhysicsEndToEndDeterminismTest
    {
        protected override IDeterminismTestSystem GetTestSystem() => DefaultWorld.GetExistingSystemManaged<HavokPhysicsDeterminismTestSystem>();
    }
}
#endif
