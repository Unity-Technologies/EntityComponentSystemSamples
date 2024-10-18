using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Entities;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor.Profiling;
using UnityEditorInternal;
#endif

namespace Unity.Physics.Tests.PerformanceTests
{
    internal class PerformanceTestUtils
    {
        public const int k_PhysicsFrameCount = 800;

        public static IEnumerator RunTest(int frameCount, List<SampleGroup> sampleGroups, List<string> lowLevelMarkers)
        {
            const int kWarmupCount = 10;
            if (lowLevelMarkers.Count == 0)
            {
                yield return Measure.Frames()
                    .ProfilerMarkers(sampleGroups.ToArray())
                    .WarmupCount(kWarmupCount)
                    .MeasurementCount(frameCount)
                    .Run();
            }
            else
            {
                int maxAttempts = 5;
                int attempt = 0;
                do
                {
                    for (var i = 0; i < kWarmupCount; ++i)
                    {
                        yield return null;
                    }
                }
                while (!Application.isPlaying && PerformanceTest.Active == null && attempt++ < maxAttempts);

                var frameSampleGroup = new SampleGroup("FrameTime");
                // sample time for requested profiler markers
                using (Measure.ProfilerMarkers(sampleGroups.ToArray()))
                {
                    for (var i = 0; i < frameCount; ++i)
                    {
                        // sample frame time
                        using (Measure.Scope(frameSampleGroup))
                        {
                            yield return null;
                        }

#if false // DOTS-10456: disabled for now until we can get a way to obtain this data reliably.
#if UNITY_EDITOR
                        // add low level marker timings
                        foreach (var marker in lowLevelMarkers)
                        {
                            var accumulatedTime = GetAccumulatedTime(marker);
                            Measure.Custom(marker, accumulatedTime);
                        }
#endif
#endif
                    }

                    // Note taken from FramesMeasurement.Run() in package com.unity.test-framework.performance@3.0.3,
                    // which this function here is inspired from:
                    // WaitForEndOfFrame coroutine is not invoked on the editor in batch mode
                    // This may lead to unexpected behavior and is better to avoid
                    // https://docs.unity3d.com/ScriptReference/WaitForEndOfFrame.html
                    if (!Application.isBatchMode && Application.isPlaying)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
        }

#if UNITY_EDITOR
        static bool GetAccmulatedTimeAtFrame(string markerName, int frameIndex, out float accumulatedTime)
        {
            accumulatedTime = 0;
            bool dataFound = false;
            for (int j = 0;; ++j)
            {
                using RawFrameDataView frame = ProfilerDriver.GetRawFrameDataView(frameIndex, j);
                if (!frame.valid)
                    break;

                var markerId = frame.GetMarkerId(markerName);
                for (int i = 0; i < frame.sampleCount; ++i)
                {
                    if (markerId != frame.GetSampleMarkerId(i))
                        continue;

                    dataFound = true;
                    var time = frame.GetSampleTimeMs(i);
                    accumulatedTime += time;
                }
            }

            return dataFound;
        }

        /// <summary>
        /// Get accumulated time for a specific marker in the last simulation frame using raw frame data access.
        /// This is currently required to obtain timing data for C# jobs.
        /// </summary>
        /// <param name="markerName">Name of the profiler marker to access time of</param>
        /// <returns>Accumulated time in ms</returns>
        static float GetAccumulatedTime(string markerName)
        {
            bool dataFound = false;

            float accumulatedTime = 0;
            int frameIndex = Time.frameCount + 1;
            do
            {
                dataFound = GetAccmulatedTimeAtFrame(markerName, frameIndex--, out accumulatedTime);
                // If no data was found in the current frame, try the previous frame.
                // The data is recorded asynchronously and might not be available yet.
                // We do recognize that this is not ideal and will potentially cause repetitions in the data readings,
                // but the results would still be representative of the performance of the system.
            }
            while (!dataFound && frameIndex > -1);
            if (!dataFound)
            {
                Debug.LogWarning($"No data found for marker {markerName}");
            }

            return accumulatedTime;
        }

#endif

        public static IEnumerable GetPerformanceTestData()
        {
            var scenes = new List<TestFixtureData>();

            var sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                if (scenePath.Contains("Tests/Performance"))
                {
#if UNITY_STANDALONE_LINUX
                    // [DOTS-9376] Currently hanging with Unity.Physics.Tests.PerformanceTests.Havok_PerformanceTest_Parallel.LoadScenes
                    if (scenePath.Contains("/ConvexCollisionPerformanceTest.unity") ||
                        scenePath.Contains("/RagdollPerformanceTest.unity") ||
                        scenePath.Contains("/SphereCollisionPerformanceTest.unity") ||
                        scenePath.Contains("/CubeCollisionPerformanceTest.unity"))
                        continue;
#endif
#if UNITY_IOS
                    if (scenePath.Contains("/RagdollPerformanceTest.unity"))
                        continue;
#endif
#if UNITY_PS4
                    // DOTS-10401 - Failing on PS4
                    if (scenePath.Contains("/TreeLifetimePerformanceTest.unity"))
                        continue;
#endif
                    var sceneName = Path.GetFileName(scenePath);
                    scenes.Add(new TestFixtureData(sceneName, scenePath));

                    // add another test case with incremental static broadphase enabled for this scene
                    if (scenePath.Contains("/TreeLifetimePerformanceTest.unity"))
                    {
                        scenes.Add(new TestFixtureData(sceneName, scenePath, Tuple.Create("Incremental Static Broadphase", true)));
                    }
                }
            }

            return scenes;
        }

        public static void GetProfilingRequestInfo(ref List<SampleGroup> sampleGroups, ref List<string> lowLevelMarkers, bool havokPerformance = false)
        {
            sampleGroups.Add(new SampleGroup("Default World Unity.Entities.FixedStepSimulationSystemGroup"));
            sampleGroups.Add(new SampleGroup(PhysicsPerformanceTestsSystem.k_PhysicsContactCountName, SampleUnit.Byte));

#if UNITY_EDITOR
            if (!havokPerformance)
            {
                lowLevelMarkers.Add("Broadphase:StaticVsDynamicFindOverlappingPairsJob (Burst)");
                lowLevelMarkers.Add("Broadphase:DynamicVsDynamicFindOverlappingPairsJob (Burst)");
                lowLevelMarkers.Add("DispatchPairSequencer:CreateDispatchPairPhasesJob (Burst)");
                lowLevelMarkers.Add("NarrowPhase:ParallelCreateContactsJob (Burst)");
                lowLevelMarkers.Add("Solver:ParallelBuildJacobiansJob (Burst)");
                lowLevelMarkers.Add("Solver:ParallelSolverJob (Burst)");
            }
            else
            {
                lowLevelMarkers.Add("HavokSimulation:StepJob (Burst)");
            }
#endif
        }
    }

    [TestFixtureSource(typeof(PerformanceTestUtils), nameof(PerformanceTestUtils.GetPerformanceTestData))]
    internal class PerformanceTestFixture : UnityPhysicsSamplesTest
    {
        readonly string m_ScenePath;
        readonly bool m_IncrementalStaticBroadphase;

        public PerformanceTestFixture(string sceneName, string scenePath)
        {
            m_ScenePath = scenePath;
            m_IncrementalStaticBroadphase = false;
        }

        public PerformanceTestFixture(string sceneName, string scenePath,
                                      Tuple<string, bool> incrementalStaticBroadphaseNamedParam)
        {
            m_ScenePath = scenePath;
            m_IncrementalStaticBroadphase = incrementalStaticBroadphaseNamedParam.Item2;
        }

        [UnityTest, Performance]
        [Timeout(10000000)]
        public IEnumerator UnityPhysicsTest()
        {
            ConfigureSimulation(World.DefaultGameObjectInjectionWorld, SimulationType.UnityPhysics,
                multiThreaded: true, incrementalStaticBroadphase: m_IncrementalStaticBroadphase);

            SceneManager.LoadScene(m_ScenePath);

            var sampleGroups = new List<SampleGroup>();
            var lowLevelProfilingMarkers = new List<string>();
            PerformanceTestUtils.GetProfilingRequestInfo(ref sampleGroups, ref lowLevelProfilingMarkers);

            return PerformanceTestUtils.RunTest(PerformanceTestUtils.k_PhysicsFrameCount, sampleGroups, lowLevelProfilingMarkers);
        }

#if HAVOK_PHYSICS_EXISTS
        [UnityTest, Performance]
        [Timeout(10000000)]
        public IEnumerator HavokTest()
        {
            ConfigureSimulation(World.DefaultGameObjectInjectionWorld, SimulationType.HavokPhysics,
                multiThreaded: true, incrementalStaticBroadphase: m_IncrementalStaticBroadphase);

            SceneManager.LoadScene(m_ScenePath);

            var sampleGroups = new List<SampleGroup>();
            var lowLevelProfilingMarkers = new List<string>();
            PerformanceTestUtils.GetProfilingRequestInfo(ref sampleGroups, ref lowLevelProfilingMarkers, havokPerformance: true);

            return PerformanceTestUtils.RunTest(PerformanceTestUtils.k_PhysicsFrameCount, sampleGroups, lowLevelProfilingMarkers);
        }

#endif
    }
}
