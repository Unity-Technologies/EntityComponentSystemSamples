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
    public interface IDeterminismTestSystem
    {
        void BeginTest();
        bool TestingFinished();
    }

    [UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class UnityPhysicsDeterminismTestSystem : JobComponentSystem, IDeterminismTestSystem
    {
        protected BuildPhysicsWorld m_BuildPhysicsWorld;
        protected StepPhysicsWorld m_StepPhysicsWorld;
        protected ExportPhysicsWorld m_ExportPhysicsWorld;
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
            m_TestingFinished = false;
        }

        public bool TestingFinished() => m_TestingFinished;

        protected override void OnCreate()
        {
            Enabled = false;

            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_ExportPhysicsWorld = World.GetOrCreateSystem<ExportPhysicsWorld>();
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

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulatedFramesInCurrentTest++;
            inputDeps = JobHandle.CombineDependencies(inputDeps, m_ExportPhysicsWorld.FinalJobHandle);

            if (SimulatedFramesInCurrentTest == k_TestDurationInFrames)
            {
                inputDeps.Complete();
                FinishTesting();
            }

            return inputDeps;
        }

    }

    // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR
    [TestFixture]
#endif
    class UnityPhysicsEndToEndDeterminismTest
    {
        private BuildPhysicsWorld m_BuildPhysicsWorld;
        private StepPhysicsWorld m_StepPhysicsWorld;
        private ExportPhysicsWorld m_ExportPhysicsWorld;

        protected static World DefaultWorld => World.DefaultGameObjectInjectionWorld;
        protected const int k_BusyWaitPeriodInSeconds = 1;

        protected virtual IDeterminismTestSystem GetTestSystem() => DefaultWorld.GetOrCreateSystem<UnityPhysicsDeterminismTestSystem>();

        protected static void SwitchWorlds()
        {
            var entityManager = DefaultWorld.EntityManager;
            var entities = entityManager.GetAllEntities();
            entityManager.DestroyEntity(entities);
            entities.Dispose();

            if (DefaultWorld.IsCreated)
            {
                var systems = DefaultWorld.Systems;
                foreach (var s in systems)
                {
                    s.Enabled = false;
                }
                DefaultWorld.Dispose();
            }

            DefaultWorldInitialization.Initialize("Default World", false);
        }

        // Demos that make no sense to be tested for determinism
        private static string[] s_FilteredOutDemos =
        {
            "InitTestScene", "LoaderScene", "SingleThreadedRagdoll"
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

        protected void DisablePhysicsSystems()
        {
            m_BuildPhysicsWorld.Enabled = false;
            m_StepPhysicsWorld.Enabled = false;
            m_ExportPhysicsWorld.Enabled = false;
        }

        public void StartTest(string scenePath)
        {
            SceneManager.LoadScene(scenePath);
            GetTestSystem().BeginTest();
        }

        public List<RigidTransform> EndTest()
        {
            var world = m_BuildPhysicsWorld.PhysicsWorld;

            List<RigidTransform> results = new List<RigidTransform>();

            for (int i = 0; i < world.NumBodies; i++)
            {
                results.Add(world.Bodies[i].WorldFromBody);
            }

            SwitchWorlds();

            return results;
        }

        [TearDown]
        public void TearDown()
        {
            SwitchWorlds();
        }

        // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR
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
                m_BuildPhysicsWorld = DefaultWorld.GetOrCreateSystem<BuildPhysicsWorld>();
                m_StepPhysicsWorld = DefaultWorld.GetOrCreateSystem<StepPhysicsWorld>();
                m_ExportPhysicsWorld = DefaultWorld.GetOrCreateSystem<ExportPhysicsWorld>();
                var testSystem = GetTestSystem();

                // Disable the systems
                DisablePhysicsSystems();

                // Wait for running systems to finish
                yield return null;

                // Load the scene

                StartTest(scenePath);

                while (!testSystem.TestingFinished())
                {
                    yield return new WaitForSeconds(k_BusyWaitPeriodInSeconds);
                }

                expected = EndTest();

            }

            // Second run
            {
                m_BuildPhysicsWorld = DefaultWorld.GetOrCreateSystem<BuildPhysicsWorld>();
                m_StepPhysicsWorld = DefaultWorld.GetOrCreateSystem<StepPhysicsWorld>();
                m_ExportPhysicsWorld = DefaultWorld.GetOrCreateSystem<ExportPhysicsWorld>();
                var testSystem = GetTestSystem();

                DisablePhysicsSystems();

                yield return null;

                // Load the scene

                StartTest(scenePath);

                while (!testSystem.TestingFinished())
                {
                    yield return new WaitForSeconds(k_BusyWaitPeriodInSeconds);
                }

                actual = EndTest();

            }

            // Compare results
            {
                // Verification
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
