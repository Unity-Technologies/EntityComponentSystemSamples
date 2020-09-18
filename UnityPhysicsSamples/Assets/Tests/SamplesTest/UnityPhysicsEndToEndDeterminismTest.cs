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

    // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR && UNITY_PHYSICS_INCLUDE_SLOW_TESTS
    [TestFixture]
#endif
    class UnityPhysicsEndToEndDeterminismTest
    {
        private BuildPhysicsWorld m_BuildPhysicsWorld;
        private StepPhysicsWorld m_StepPhysicsWorld;
        private ExportPhysicsWorld m_ExportPhysicsWorld;

        protected static World DefaultWorld => World.DefaultGameObjectInjectionWorld;
        protected const int k_BusyWaitPeriodInSeconds = 1;

        protected virtual IDeterminismTestSystem GetTestSystem() => DefaultWorld.GetExistingSystem<UnityPhysicsDeterminismTestSystem>();

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
                ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(DefaultWorld);
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

        protected IEnumerator LoadSceneWithDeferredSwitchWorlds(string scenePath)
        {
            // Load the scene asynchronously and defer the SwitchWorlds() call until
            // just before the Scene is activated, to guarantee that GameObject-to-Entity
            // conversion happens at a predictable time.
            LoadSceneParameters loadParameters = new LoadSceneParameters(LoadSceneMode.Single);
            var sceneLoadOp = SceneManager.LoadSceneAsync(scenePath, loadParameters);
            sceneLoadOp.allowSceneActivation = false;
            while (!sceneLoadOp.isDone)
            {
                if (sceneLoadOp.progress >= 0.9f)
                {
                    SwitchWorlds();
                    sceneLoadOp.allowSceneActivation = true;
                }
                yield return null;
            }
        }

        protected void DisablePhysicsSystems()
        {
            m_BuildPhysicsWorld.Enabled = false;
            m_StepPhysicsWorld.Enabled = false;
            m_ExportPhysicsWorld.Enabled = false;
        }

        public void StartTest()
        {
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
                m_BuildPhysicsWorld = DefaultWorld.GetOrCreateSystem<BuildPhysicsWorld>();
                m_StepPhysicsWorld = DefaultWorld.GetOrCreateSystem<StepPhysicsWorld>();
                m_ExportPhysicsWorld = DefaultWorld.GetOrCreateSystem<ExportPhysicsWorld>();

                // Disable the systems
                DisablePhysicsSystems();

                // Wait for running systems to finish
                yield return null;

                // Load the scene (and wait for it to load)
                yield return LoadSceneWithDeferredSwitchWorlds(scenePath);

                var testSystem = GetTestSystem();
                StartTest();

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
                DisablePhysicsSystems();

                yield return null;

                // Load the scene (and wait for it to load)
                yield return LoadSceneWithDeferredSwitchWorlds(scenePath);

                var testSystem = GetTestSystem();
                StartTest();

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
