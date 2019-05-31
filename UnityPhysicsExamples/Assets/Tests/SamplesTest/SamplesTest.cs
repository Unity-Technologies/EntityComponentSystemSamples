using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Physics.Samples.Test
{
    [TestFixture]
    public class UnityPhysicsSamplesTest
    {
        protected static IEnumerable GetScenes()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var scenes = new List<string>();
            for(int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                if (scenePath.Contains("InitTestScene"))
                    continue;

                scenes.Add(scenePath);
            }
            scenes.Sort();
            return scenes;
        }

        [UnityTest]
        [Timeout(60000)]
        public virtual IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            EntitiesCleanup();
            yield return new WaitForFixedUpdate();
            LogAssert.NoUnexpectedReceived();
        }

        [TearDown]
        public void TearDown()
        {
            EntitiesCleanup();
        }

        protected static void EntitiesCleanup()
        {
            var entityManager = World.Active.EntityManager;
            var entities = entityManager.GetAllEntities();
            entityManager.DestroyEntity(entities);
            entities.Dispose();
        }
    }
}
