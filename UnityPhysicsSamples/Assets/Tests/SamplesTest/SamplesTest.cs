using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Physics.Systems;

namespace Unity.Physics.Samples.Test
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    class EnsureSTSimulation : SystemBase
    {
        protected override void OnUpdate()
        {
            if (HasSingleton<PhysicsStep>())
            {
                var component = GetSingleton<PhysicsStep>();
                if (component.MultiThreaded > 0)
                {
                    component.MultiThreaded = 0;
                    SetSingleton(component);
                }
            }
            else
            {
                // Invalidate the physics world
                var buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BuildPhysicsWorld>();
                buildPhysicsWorld.PhysicsWorld.Reset(0, 0, 0);
            }
        }
    }

    [TestFixture]
    abstract class UnityPhysicsSamplesTest
    {
        static World DefaultWorld => World.DefaultGameObjectInjectionWorld;

        protected static IEnumerable GetScenes()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var scenes = new List<string>();
            for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                if (scenePath.Contains("InitTestScene")
                    // in order to circumvent API breakages that do not affect physics, some packages are removed from the project on CI
                    // any scenes referencing asset types in com.unity.inputsystem must be guarded behind UNITY_INPUT_SYSTEM_EXISTS
#if !UNITY_INPUT_SYSTEM_EXISTS
                    || scenePath.Contains("LoaderScene")
#endif
                )
                    continue;
#if UNITY_ANDROID && !UNITY_64
                // Terrain scene needs a lot of memory, skip it on Android armv7
                if (scenePath.Contains("/Terrain.unity"))
                    continue;
#endif

                scenes.Add(scenePath);
            }
            scenes.Sort();
            return scenes;
        }

        [UnityTest]
        [Timeout(240000)]
        public abstract IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath);

        [TearDown]
        public void TearDown()
        {
            ResetDefaultWorld();
        }

        protected static void ResetDefaultWorld()
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
    }

    [TestFixture]
    class UnityPhysicsSamplesTestMT : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(240000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            ResetDefaultWorld();
            yield return new WaitForFixedUpdate();

            LogAssert.NoUnexpectedReceived();
        }
    }

    [TestFixture]
    class UnityPhysicsSamplesTestST : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(240000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            var world = World.DefaultGameObjectInjectionWorld;

            // Ensure ST simulation
            var stSystem = world.GetExistingSystem<EnsureSTSimulation>();
            {
                Assert.IsNull(stSystem, "The 'EnsureSTSimulation' system should only be created by the 'SamplesTest.LoadScenes' function!");

                stSystem = new EnsureSTSimulation();
                world.AddSystem(stSystem);
                world.GetExistingSystem<FixedStepSimulationSystemGroup>().AddSystemToUpdateList(stSystem);
            }

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            ResetDefaultWorld();
            yield return new WaitForFixedUpdate();

            LogAssert.NoUnexpectedReceived();
        }
    }
}
