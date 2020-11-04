using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Jobs;

namespace Unity.Physics.Samples.Test
{
    interface IDeterminismTestSystem
    {
        void BeginTest();
        bool TestingFinished();
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
    class UnityPhysicsDeterminismTestSystem : SystemBase, IDeterminismTestSystem
    {
        protected BuildPhysicsWorld m_BuildPhysicsWorld;
        protected StepPhysicsWorld m_StepPhysicsWorld;
        protected ExportPhysicsWorld m_ExportPhysicsWorld;
        protected FixedStepSimulationSystemGroup m_FixedStepGroup;

        protected bool m_TestingFinished = false;
        protected bool m_RecordingBegan = false;

        public int SimulatedFramesInCurrentTest = 0;
        public const int k_TestDurationInFrames = 100;

        public void BeginTest()
        {
            SimulatedFramesInCurrentTest = 0;
            Enabled = true;
            m_ExportPhysicsWorld.Enabled = true;
            m_StepPhysicsWorld.Enabled = true;
            m_BuildPhysicsWorld.Enabled = true;
            m_FixedStepGroup.Enabled = true;
            m_TestingFinished = false;
        }

        public bool TestingFinished() => m_TestingFinished;

        protected override void OnCreate()
        {
            Enabled = false;

            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_ExportPhysicsWorld = World.GetOrCreateSystem<ExportPhysicsWorld>();
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
                var handle = JobHandle.CombineDependencies(Dependency, m_ExportPhysicsWorld.GetOutputDependency());

                if (SimulatedFramesInCurrentTest == k_TestDurationInFrames)
                {
                    handle.Complete();
                    FinishTesting();
                }

                Dependency = handle;
            }
        }
    }

    // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR && UNITY_PHYSICS_INCLUDE_SLOW_TESTS
    [TestFixture]
#endif
    class UnityPhysicsEndToEndDeterminismTest
    {
        protected static World DefaultWorld => World.DefaultGameObjectInjectionWorld;
        protected const int k_BusyWaitPeriodInSeconds = 1;

        protected virtual IDeterminismTestSystem GetTestSystem() => DefaultWorld.GetExistingSystem<UnityPhysicsDeterminismTestSystem>();

        protected static void SwitchWorlds()
        {
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            defaultWorld.EntityManager.CompleteAllJobs();
            foreach (var system in defaultWorld.Systems)
            {
                system.Enabled = false;
            }

            defaultWorld.Dispose();
            DefaultWorldInitialization.Initialize("Default World", false);
        }

        // Demos that make no sense to be tested for determinism
        private static string[] s_FilteredOutDemos =
        {
            "InitTestScene", "LoaderScene", "SingleThreadedRagdoll",

            // Removing 1c. Conversion since it has no ECS data, it would cause timeouts in EndToEndDeterminismTest
            "1c. Conversion"
        };

        protected static IEnumerable GetScenes()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var scenes = new List<string>();

            for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);

                bool addScene = true;

                for (int i = 0; i < s_FilteredOutDemos.Length; i++)
                {
                    if (scenePath.Contains(s_FilteredOutDemos[i]))
                    {
                        addScene = false;
                        break;
                    }
                }

                if (addScene)
                {
                    scenes.Add(scenePath);
                }
            }

            scenes.Sort();
            return scenes;
        }

        protected void LoadSceneIntoNewWorld(string scenePath)
        {
            SwitchWorlds();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BuildPhysicsWorld>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<StepPhysicsWorld>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ExportPhysicsWorld>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<FixedStepSimulationSystemGroup>().Enabled = false;

            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
        }

        public void StartTest()
        {
            GetTestSystem().BeginTest();
        }

        public List<RigidTransform> EndTest()
        {
            var world = DefaultWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

            List<RigidTransform> results = new List<RigidTransform>();

            for (int i = 0; i < world.NumBodies; i++)
            {
                results.Add(world.Bodies[i].WorldFromBody);
            }

            return results;
        }

        [TearDown]
        public void TearDown()
        {
            SwitchWorlds();
        }

        // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR && UNITY_PHYSICS_INCLUDE_SLOW_TESTS
        [UnityTest]
#endif
        public virtual IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            List<RigidTransform> expected = null;
            List<RigidTransform> actual = null;

            // First run
            {
                // Load scene
                LoadSceneIntoNewWorld(scenePath);

                // Wait for ECS to finish distributing entities on chunks
                // Todo: find a better solution for this
                yield return new WaitForSeconds(1);

                StartTest();

                var testSystem = GetTestSystem();

                while (!testSystem.TestingFinished())
                {
                    yield return new WaitForSeconds(k_BusyWaitPeriodInSeconds);
                }

                expected = EndTest();
            }

            // Second run
            {
                //Load scene
                LoadSceneIntoNewWorld(scenePath);

                // Wait for ECS to finish distributing entities on chunks
                // Todo: find a better solution for this
                yield return new WaitForSeconds(1);

                StartTest();

                var testSystem = GetTestSystem();

                while (!testSystem.TestingFinished())
                {
                    yield return new WaitForSeconds(k_BusyWaitPeriodInSeconds);
                }

                actual = EndTest();
            }

            // Compare results
            {
                // Verification

                Assert.IsTrue(expected.Count > 0);
                Assert.IsTrue(actual.Count > 0);
                Assert.IsTrue(expected.Count == actual.Count);

                int numSame = 0;
                int numBodies = expected.Count;
                for (int i = 0; i < numBodies; i++)
                {
                    if (math.all(expected[i].pos == actual[i].pos) && math.all(expected[i].rot.value == actual[i].rot.value))
                    {
                        numSame++;
                    }
                    else
                    {
                        if (!math.all(expected[i].pos == actual[i].pos))
                        {
                            Debug.Log($"Expected Position: {expected[i].pos}, Actual: {actual[i].pos}, RigidBodyIndex: {i}");
                        }
                        if (!math.all(expected[i].rot.value == actual[i].rot.value))
                        {
                            Debug.Log($"Expected Rotation: {expected[i].rot.value}, Actual: {actual[i].rot.value}, RigidBodyIndex: {i}");
                        }
                    }
                }

                Assert.AreEqual(numBodies, numSame, "Not all bodies have the same transform!");

                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}
