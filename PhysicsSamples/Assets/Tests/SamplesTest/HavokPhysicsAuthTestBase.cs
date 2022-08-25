using NUnit.Framework;
using System.Collections;
using Unity.Entities;
using Unity.Physics.Samples.Test;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Generics = System.Collections.Generic;

#if HAVOK_PHYSICS_EXISTS

// Base class for Authorization tests. It just loads the the Hello World scene and ensures that Havok physics is used.
// Classes that inherit this class should implement all the authentication scenarios and validate the result.
abstract class HavokPhysicsAuthTestBase : UnityPhysicsSamplesTest
{
    protected static IEnumerable GetHelloWorldScene()
    {
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        var scenes = new Generics.List<string>();
        for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            if (scenePath.Contains("Hello World"))
            {
                scenes.Add(scenePath);
            }
        }
        return scenes;
    }

    int m_NumTestsRun;

    // Indicates whether we should suppress dialogs and show authentication errors in console instead
    protected bool m_suppressDialogs = true;

    [UnityTest]
    [Timeout(240000)]
    public override IEnumerator LoadScenes([ValueSource(nameof(GetHelloWorldScene))] string scenePath)
    {
        // Log warnings instead of creating dialog boxes if authentication fails
        if (m_suppressDialogs)
        {
            PlayerPrefs.SetInt("Havok.Auth.SuppressDialogs", 1);
        }
        else
        {
            PlayerPrefs.DeleteKey("Havok.Auth.SuppressDialogs");
        }

        // Ensure Havok
        var world = World.DefaultGameObjectInjectionWorld;
        var system = world.GetOrCreateSystem<EnsureHavokSystem>();
        EnsureHavokSystem.EnsureHavok(system);

        SceneManager.LoadScene(scenePath);
        yield return new WaitForSeconds(1);
        UnityPhysicsSamplesTest.ResetDefaultWorld();
        yield return new WaitForFixedUpdate();

        LogAssert.NoUnexpectedReceived();
        m_NumTestsRun++;

        PlayerPrefs.DeleteKey("Havok.Auth.SuppressDialogs");
    }
}

#endif
