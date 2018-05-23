using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[TestFixture]
[UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor, RuntimePlatform.WindowsEditor)]
public class SceneLoadingTests
{
    [UnityTest]
    public IEnumerator LoadScenes_NoScenesShouldLog()
    {
        var sceneCount = SceneManager.sceneCountInBuildSettings;

        var sceneIndex = 1; // Skip the SceneSwitcher scene which is always first
        while (sceneIndex < sceneCount)
        {
            var nextScene = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            if (!nextScene.StartsWith("Assets/Scenes/") || nextScene.Contains("SceneSwitcher"))
            {
                sceneIndex++;
                continue;
            }
            EntitiesCleanup();

            SceneManager.LoadScene(nextScene);
            yield return new WaitForSeconds(1);
            sceneIndex++;
            LogAssert.NoUnexpectedReceived();
        }
    }

    [TearDown]
    public void TearDown()
    {
        EntitiesCleanup();
    }

    static void EntitiesCleanup()
    {
        var entityManager = World.Active.GetExistingManager<EntityManager>();
        var entities = entityManager.GetAllEntities();
        entityManager.DestroyEntity(entities);
        entities.Dispose();
    }
}
