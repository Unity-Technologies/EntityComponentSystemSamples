//This script is copied from UniversalRP TestProject
// https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/master/TestProjects/UniversalGraphicsTest/Assets/Test/Runtime/UniversalGraphicsTests.cs

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Scenes;

public class GraphicsTests
{
    #if UNITY_ANDROID
        static bool wasFirstSceneRan = false;
        const int firstSceneAdditionalFrames = 3;
    #endif
    public const string path = "Assets/ReferenceImages";

    [UnityTest, Category("Custom Graphics Tests")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        CleanUp();

        if (!testCase.ScenePath.Contains("GraphicsTest"))
        {
            Assert.Ignore("Ignoring this test because the scene is not under GraphicsTest folder, or not named with GraphicsTest.");
        }

        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        //Get Test settings
        //ignore instead of failing, because some scenes might not be used for GraphicsTest
        var settings = Object.FindObjectOfType<GraphicsTestSettingsCustom>();
        if (settings == null) Assert.Ignore("Ignoring this test for GraphicsTest because couldn't find GraphicsTestSettingsCustom");

        #if !UNITY_EDITOR
        Screen.SetResolution(settings.ImageComparisonSettings.TargetWidth, settings.ImageComparisonSettings.TargetHeight, false);
        #endif

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x=>x.GetComponent<Camera>());
        //var settings = Object.FindObjectOfType<UniversalGraphicsTestSettings>();
        //Assert.IsNotNull(settings, "Invalid test scene, couldn't find UniversalGraphicsTestSettings");        

        //Scene scene = SceneManager.GetActiveScene();

        yield return null;

        int waitFrames = settings.WaitFrames;

        if (settings.ImageComparisonSettings.UseBackBuffer && settings.WaitFrames < 1)
        {
            waitFrames = 1;
        }

        for (int i = 0; i < waitFrames; i++)
            yield return new WaitForEndOfFrame();

        #if UNITY_ANDROID
        // On Android first scene often needs a bit more frames to load all the assets
        // otherwise the screenshot is just a black screen
        if (!wasFirstSceneRan)
        {
            for(int i = 0; i < firstSceneAdditionalFrames; i++)
            {
                yield return null;
            }
            wasFirstSceneRan = true;
        }
        #endif

        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings);
    }

    [TearDown]
    public void DumpImagesInEditor()
    {
        #if UNITY_EDITOR
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
        #endif

        // foreach (GameObject o in Object.FindObjectsOfType<GameObject>()) 
        // {
        //     Object.Destroy(o);
        // }

        CleanUp();

    }

    public void CleanUp()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;
        var entities = entityManager.GetAllEntities();

        for (int i = 0; i < entities.Length; i++)
        {
            string ename = entityManager.GetName(entities[i]);
            bool isSubscene = entityManager.HasComponent<SubScene>(entities[i]);
            bool isSceneSection = entityManager.HasComponent<SceneSection>(entities[i]);

            //Runtime generated entities requires manual deletion, 
            //but we need to skip for some specific entities otherwise there will be spamming error
            if( ename != "SceneSectionStreamingSingleton" && !isSubscene && !isSceneSection && !ename.Contains("GameObject Scene:") )
            {
                entityManager.DestroyEntity(entities[i]);
            }
        }
    }

}