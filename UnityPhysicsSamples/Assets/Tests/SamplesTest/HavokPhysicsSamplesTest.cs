using NUnit.Framework;
using System.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Physics.Samples.Test
{
#if HAVOK_PHYSICS_EXISTS
    [TestFixture]
    class HavokPhysicsSamplesTestMT : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(60000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(UnityPhysicsSamplesTest.GetScenes))] string scenePath)
        {
            // Don't create log messages about the number of trial days remaining
            PlayerPrefs.SetInt("Havok.Auth.SuppressDialogs", 1);

            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            // Ensure Havok
            var world = World.DefaultGameObjectInjectionWorld;

            var system = world.GetOrCreateSystem<EnsureHavokSystem>();
            system.Enabled = true;

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            UnityPhysicsSamplesTest.SwitchWorlds();
            yield return new WaitForFixedUpdate();

            LogAssert.NoUnexpectedReceived();

            PlayerPrefs.DeleteKey("Havok.Auth.SuppressDialogs");
        }
    }

    [TestFixture]
    class HavokPhysicsSamplesTestST : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(60000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(UnityPhysicsSamplesTest.GetScenes))] string scenePath)
        {
            // Don't create log messages about the number of trial days remaining
            PlayerPrefs.SetInt("Havok.Auth.SuppressDialogs", 1);

            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            // Ensure Havok
            var world = World.DefaultGameObjectInjectionWorld;
            var system = world.GetOrCreateSystem<EnsureHavokSystem>();
            system.Enabled = true;

            // Ensure ST simulation
            var stSystem = world.GetExistingSystem<EnsureSTSimulation>();
            {
                Assert.IsNull(stSystem, "The 'EnsureSTSimulation' system should only be created by the 'HavokPhysicsSamplesTest.LoadScenes' function!");

                stSystem = new EnsureSTSimulation();
                world.AddSystem(stSystem);
                world.GetExistingSystem<SimulationSystemGroup>().AddSystemToUpdateList(stSystem);
            }

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            UnityPhysicsSamplesTest.SwitchWorlds();
            yield return new WaitForFixedUpdate();

            LogAssert.NoUnexpectedReceived();

            PlayerPrefs.DeleteKey("Havok.Auth.SuppressDialogs");
        }
    }
#endif
}
