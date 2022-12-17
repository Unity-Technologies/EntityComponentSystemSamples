using NUnit.Framework;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Physics.Tests
{
#if HAVOK_PHYSICS_EXISTS && (UNITY_EDITOR || UNITY_ANDROID ||  UNITY_IOS || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)

    [TestFixture]
    class HavokPhysicsSamplesTestMT : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(240000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(UnityPhysicsSamplesTest.GetScenes))] string scenePath)
        {
            // Don't create log messages about the number of trial days remaining
            PlayerPrefs.SetInt("Havok.Auth.SuppressDialogs", 1);

            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            // Enable multi threaded Havok simulation
            ConfigureSimulation(World.DefaultGameObjectInjectionWorld, SimulationType.HavokPhysics, true);

            yield return LoadSceneAndSimulate(scenePath);

            LogAssert.NoUnexpectedReceived();

            PlayerPrefs.DeleteKey("Havok.Auth.SuppressDialogs");
        }
    }

    [TestFixture]
    class HavokPhysicsSamplesTestST : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(240000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(UnityPhysicsSamplesTest.GetScenes))] string scenePath)
        {
            // Don't create log messages about the number of trial days remaining
            PlayerPrefs.SetInt("Havok.Auth.SuppressDialogs", 1);

            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            // Enable single threaded Havok simulation
            ConfigureSimulation(World.DefaultGameObjectInjectionWorld, SimulationType.HavokPhysics, false);

            yield return LoadSceneAndSimulate(scenePath);

            LogAssert.NoUnexpectedReceived();

            PlayerPrefs.DeleteKey("Havok.Auth.SuppressDialogs");
        }
    }

#endif
}
