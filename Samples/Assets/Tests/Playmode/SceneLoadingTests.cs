﻿using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[TestFixture]
public class SceneLoadingTests
{
    private GameObject sceneSwitcherDummy;

    private static IEnumerable GetScenes()
    {
        var sceneCount = SceneManager.sceneCountInBuildSettings;

        var sceneIndex = 1; // Skip the SceneSwitcher scene which is always first
        while (sceneIndex < sceneCount)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            if (!scenePath.StartsWith("Assets/Scenes/") || scenePath.Contains("InitTestScene") || scenePath.Contains("SceneSwitcher"))
            {
                sceneIndex++;
                continue;
            }

            var fileName = scenePath.Substring(scenePath.LastIndexOf("/") + 1);
            var sceneName = fileName.Substring(0, fileName.LastIndexOf(".unity"));

            yield return sceneName;
            sceneIndex++;
        }
    }

    [SetUp]
    public void Setup()
    {
        // Some scenes start auto playing if a game object called scene switcher is available when the scene is loaded
        // So that would be useful
        sceneSwitcherDummy = new GameObject("SceneSwitcher");
        GameObject.DontDestroyOnLoad(sceneSwitcherDummy);
    }

    [UnityTest]
    public IEnumerator LoadScenes_NoScenesShouldLog([ValueSource(nameof(GetScenes))] string sceneName)
    {
        SceneManager.LoadScene($"Assets/Scenes/{sceneName}.unity");
        yield return new WaitForSeconds(1);
        EntitiesCleanup();
        yield return new WaitForFixedUpdate();
        LogAssert.NoUnexpectedReceived();
    }

    [TearDown]
    public void TearDown()
    {
        GameObject.Destroy(sceneSwitcherDummy);
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
