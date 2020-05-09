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
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    class EnsureSTSimulation : ComponentSystem
    {
        protected override void OnUpdate()
        {
            if (HasSingleton<PhysicsStep>())
            {
                var component = GetSingleton<PhysicsStep>();
                if (component.ThreadCountHint != 0)
                {
                    component.ThreadCountHint = 0;
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
            for(int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                if (scenePath.Contains("InitTestScene"))
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
        [Timeout(60000)]
        public abstract IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath);

        [TearDown]
        public void TearDown()
        {
            SwitchWorlds();
        }

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
    }

    [TestFixture]
    class UnityPhysicsSamplesTestMT : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(60000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            SwitchWorlds();
            yield return new WaitForFixedUpdate();

            LogAssert.NoUnexpectedReceived();
        }
    }

    [TestFixture]
    class UnityPhysicsSamplesTestST : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(60000)]
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
                world.GetExistingSystem<SimulationSystemGroup>().AddSystemToUpdateList(stSystem);
            }

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            SwitchWorlds();
            yield return new WaitForFixedUpdate();

            LogAssert.NoUnexpectedReceived();
        }
    }
}
