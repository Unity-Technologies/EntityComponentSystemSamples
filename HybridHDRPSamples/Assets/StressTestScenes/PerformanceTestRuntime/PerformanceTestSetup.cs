using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

public class PerformanceTestSetup : IPrebuildSetup, IPostBuildCleanup
{
    #if UNITY_EDITOR
    public static List<EditorBuildSettingsScene> originalSceneList; //have to be static otherwise it will be null in Cleanup()
    public static bool isRunningPerfTest = false;
    #endif
    
    public void Setup()
    {
        #if UNITY_EDITOR

        //Check if we are running performance test
        string[] args = System.Environment.GetCommandLineArgs ();
        for (int i = 0; i < args.Length; i++) 
        {
            if(args[i].Contains("Performance"))
            {
                isRunningPerfTest = true;
                break;
            }
        }
        Debug.Log("PerformanceTestSetup - isRunningPerfTest = "+isRunningPerfTest);

        if(isRunningPerfTest)
        {
            originalSceneList = new List<EditorBuildSettingsScene>();

            string[] scenesToTest = PerformanceTests.ScenesToTest();
            Debug.Log("PerformanceTestSetup - scenesToTest contains "+scenesToTest.Length+" scenes.");

            //Make a list of performance test scenes
            List<EditorBuildSettingsScene> perfTestSceneList = new List<EditorBuildSettingsScene>();
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                //Add scene to original scene list
                originalSceneList.Add(EditorBuildSettings.scenes[i]);

                //Check scene is performance test and add scene
                bool isPerfTestScene = scenesToTest.Any(EditorBuildSettings.scenes[i].path.Contains);
                if(isPerfTestScene)
                {
                    perfTestSceneList.Add(EditorBuildSettings.scenes[i]);
                }
            }

            //Only add performance test scenes to BuildSettings
            EditorBuildSettings.scenes = perfTestSceneList.ToArray();
            Debug.Log("PerformanceTestSetup - EditorBuildSettings.scenes now contains "+EditorBuildSettings.scenes.Length+" scenes.");
            Debug.Log("PerformanceTestSetup - originalSceneList contains "+originalSceneList.Count+" scenes.");
        }

        #endif
    }

    //Without this test function, Setup() and Cleanup() won't be triggered
    [Test, Category("Performance")]
    public void PerformanceTestSetupTest()
    {
        Assert.Ignore();
    }

    public void Cleanup()
    {
        #if UNITY_EDITOR

        if(isRunningPerfTest)
        {
            //Restore original scenelist
            EditorBuildSettings.scenes = originalSceneList.ToArray();
            Debug.Log("PerformanceTestSetup - Done clean up. EditorBuildSettings.scenes restored "+EditorBuildSettings.scenes.Length+" scenes.");
        }

        #endif
    }
}
