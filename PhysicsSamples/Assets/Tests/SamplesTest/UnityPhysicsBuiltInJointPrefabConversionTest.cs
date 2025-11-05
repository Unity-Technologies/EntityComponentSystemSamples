#if UNITY_ANDROID && !UNITY_64
#define UNITY_ANDROID_ARM7V
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Physics.Tests
{
    /// <summary>
    /// Used to test the conversion of prefabs versus instantiation for built-in components.
    /// Ensures that instantiated and non-instantiated prefab references produce consistent data,
    /// particularly verifying that <c>BodyBFromJoint</c> remains the same unless intentionally modified at runtime.
    /// </summary>
    [TestFixture]
    class UnityPhysicsBuiltInJointPrefabConversionTest : UnityPhysicsSamplesTest
    {
        private static IEnumerable GetJointScenes()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var scenes = new List<string>();
            for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                if (scenePath.Contains("Built-in Prefab Joint Conversion"))
                {
                    scenes.Add(scenePath);
                }
            }
            scenes.Sort();
            return scenes;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator LoadScenes([ValueSource(nameof(GetJointScenes))] string scenePath)
        {
            VerifyConsoleMessages.ClearMessagesInConsole();

            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            // Enable multi threaded Unity Physics simulation
            ConfigureSimulation(DefaultWorld, SimulationType.UnityPhysics);

            var simulationTime = 1.0f;

            yield return LoadScene(scenePath);
            yield return Simulate();

            using (var subSceneQuery = DefaultWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PrefabComponentData>()))
            {
                Assert.Greater(subSceneQuery.CalculateEntityCount(), 0);
                using (var sceneEntities = subSceneQuery.ToComponentDataArray<PrefabComponentData>(Allocator.Persistent))
                {
                    Assert.Greater(sceneEntities.Length, 0);
                    NativeHashMap<Entity, Entity> prefabVSInstance = new NativeHashMap<Entity, Entity>(sceneEntities.Length, Allocator.Persistent);

                    foreach (var container in sceneEntities)
                    {
                        var prefabInstantiated = DefaultWorld.EntityManager.Instantiate(container.Prefab);
                        yield return new WaitForSeconds(simulationTime);
                        prefabVSInstance.Add(container.Prefab, prefabInstantiated);
                    }

                    Assert.Greater(prefabVSInstance.Count, 0);

                    foreach (var item in prefabVSInstance)
                    {
                        var bufferPrefab = DefaultWorld.EntityManager.GetBuffer<LinkedEntityGroup>(item.Key, true);
                        var bufferInstantiated = DefaultWorld.EntityManager.GetBuffer<LinkedEntityGroup>(item.Value, true);
                        Assert.AreEqual(bufferPrefab.Length, bufferInstantiated.Length);
                        for (var i = 0; i < bufferPrefab.Length; i++)
                        {
                            if (DefaultWorld.EntityManager.HasComponent<PhysicsJoint>(bufferPrefab[i].Value))
                            {
                                var jointPrefab = DefaultWorld.EntityManager.GetComponentData<PhysicsJoint>(bufferPrefab[i].Value);
                                var instantiatedPrefab = DefaultWorld.EntityManager.GetComponentData<PhysicsJoint>(bufferInstantiated[i].Value);
                                Assert.AreEqual(jointPrefab.BodyBFromJoint.Position, instantiatedPrefab.BodyBFromJoint.Position,
                                    $"Prefab: {jointPrefab.BodyBFromJoint.Position} vs Instance:{instantiatedPrefab.BodyBFromJoint.Position}");
                            }
                        }
                    }

                    prefabVSInstance.Dispose();
                }
            }

            yield return new WaitForSeconds(simulationTime);

            ResetDefaultWorld();
            yield return new WaitForFixedUpdate();

            VerifyConsoleMessages.VerifyPrintedMessages(scenePath);
        }
    }
}
