using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.PerformanceTesting;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Physics.Tests.PerformanceTests
{
    internal class PerformanceTestUtils
    {
        public const int k_PhysicsFrameCount = 800;

        internal static IEnumerator RunTest(int frameCount, SampleGroup[] sampleGroups)
        {
            yield return Measure.Frames()
                .ProfilerMarkers(sampleGroups)
                .WarmupCount(10)
                .MeasurementCount(frameCount)
                .Run();
        }
    }

    class UnityPhysics_PerformanceTest_Parallel : UnityPhysicsSamplesTest
    {
        [SetUp]
        public void SetUp()
        {
            ConfigureSimulation(World.DefaultGameObjectInjectionWorld, SimulationType.UnityPhysics, true);
        }

        [UnityTest, Performance]
        [Timeout(10000000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(GetPerformanceScenes))] string scenePath)
        {
            SceneManager.LoadScene(scenePath);

            var sampleGroups = new[]
            {
                new SampleGroup("Default World Unity.Entities.FixedStepSimulationSystemGroup", SampleUnit.Millisecond),

                new SampleGroup("JobHandle.Complete", SampleUnit.Millisecond),
                new SampleGroup("Broadphase:StaticVsDynamicFindOverlappingPairsJob (Burst)", SampleUnit.Millisecond),
                new SampleGroup("Broadphase:DynamicVsDynamicFindOverlappingPairsJob (Burst)", SampleUnit.Millisecond),
                new SampleGroup("DispatchPairSequencer:CreateDispatchPairPhasesJob (Burst)", SampleUnit.Millisecond),
                new SampleGroup("NarrowPhase:ParallelCreateContactsJob (Burst)", SampleUnit.Millisecond),
                new SampleGroup("Solver:ParallelBuildJacobiansJob (Burst)", SampleUnit.Millisecond),
                new SampleGroup("Solver:ParallelSolverJob (Burst)", SampleUnit.Millisecond),
                new SampleGroup(PhysicsPerformanceTestsSystem.k_PhysicsContactCountName, SampleUnit.Byte)
            };
            return PerformanceTestUtils.RunTest(PerformanceTestUtils.k_PhysicsFrameCount, sampleGroups);
        }
    }

#if HAVOK_PHYSICS_EXISTS
    class Havok_PerformanceTest_Parallel : UnityPhysicsSamplesTest
    {
        [SetUp]
        public void SetUp()
        {
            ConfigureSimulation(World.DefaultGameObjectInjectionWorld, SimulationType.HavokPhysics, true);
        }

        [UnityTest, Performance]
        [Timeout(10000000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(GetPerformanceScenes))] string scenePath)
        {
            SceneManager.LoadScene(scenePath);

            var sampleGroups = new[]
            {
                new SampleGroup("Default World Unity.Entities.FixedStepSimulationSystemGroup", SampleUnit.Millisecond),
                new SampleGroup("HavokSimulation:StepJob (Burst)", SampleUnit.Millisecond),
                new SampleGroup(HavokPerformanceTestsSystem.k_PhysicsContactCountName, SampleUnit.Byte)
            };
            return PerformanceTestUtils.RunTest(PerformanceTestUtils.k_PhysicsFrameCount, sampleGroups);
        }
    }
#endif
}
