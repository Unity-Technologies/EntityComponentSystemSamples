using System.IO;
using Unity.Build;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Build.Common;
using System.Collections.Generic;

public class SetupGraphicsTestCases : IPrebuildSetup
{
    private static BuildTarget target;
    private static BuildConfiguration config;

    public void Setup()
    {
        TriggerPreparePlayerTest();

        // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
        // Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
        // can be used directly instead.
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(GraphicsTests.path);
    }

    public static void TriggerPreparePlayerTest()
    {
        var args = System.Environment.GetCommandLineArgs();
        string testType = "playmode test";
        for(int i=0; i<args.Length; i++)
        {
            //Debug
            Log("*************** SetupGraphicsTestCases - Args "+i+" = "+args[i]);

            //Tell whether yamato is running player test or playmode test
            if( args[i].Contains("Standalone") )
            {
                testType = "standalone test";
                PreparePlayerTest();
                break;
            }
        }
        Log("*************** SetupGraphicsTestCases - This is "+testType);
    }

    [MenuItem("GraphicsTest/PreparePlayerTest")]
    public static void PreparePlayerTest()
    {
        Log("*************** SetupGraphicsTestCases - Getting BuildConfig");

        //Get the correct config file
        target = EditorUserBuildSettings.activeBuildTarget;
        config = FindConfig(target);

        //Sync scenelist
        SyncSceneList(false);

        Log("*************** SetupGraphicsTestCases - Triggering BuildConfig.Build()");

        //Make the build
        config.Build();

        Log("*************** SetupGraphicsTestCases - Moving subscene cache");
        CreateFolder();
        CopyFiles();
    }

    private static void Log(string t)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, t);
    }

    private static BuildConfiguration FindConfig(BuildTarget t)
    {
        string configPath = "";
        switch (t)
        {
            case BuildTarget.StandaloneWindows: configPath = "Assets/Tests/Editor/GraphicsBuildconfig_Win.buildconfiguration"; break;
            case BuildTarget.StandaloneWindows64: configPath = "Assets/Tests/Editor/GraphicsBuildconfig_Win.buildconfiguration"; break;
            case BuildTarget.StandaloneOSX: configPath = "Assets/Tests/Editor/GraphicsBuildconfig_Mac.buildconfiguration"; break;
        }
        return (BuildConfiguration)AssetDatabase.LoadAssetAtPath(configPath, typeof(BuildConfiguration));
    }

    //Sync scenelist from BuildSettings to BuildConfig
    //Cannot automate this because Yamato complains "InvalidOperationException: Building is not allowed while Unity is compiling."
    [MenuItem("GraphicsTest/SyncSceneListToAllConfig")]
    private static void SyncSceneList()
    {
        SyncSceneList(true);
    }

    private static void SyncSceneList(bool applyToAll)
    {      
        EditorBuildSettingsScene[] buildSettingScenes = EditorBuildSettings.scenes;
        List<SceneList.SceneInfo> scenelist = new List<SceneList.SceneInfo>();
        for(int i=0;i<buildSettingScenes.Length;i++)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildSettingScenes[i].path);
            scenelist.Add(new SceneList.SceneInfo() { AutoLoad = false, Scene = GlobalObjectId.GetGlobalObjectIdSlow(sceneAsset) });
        }

        if(applyToAll)
        {
            var assets = AssetDatabase.FindAssets("t:BuildConfiguration", new[] {"Assets/Tests/Editor"});
            foreach (var guid in assets) 
            {
                var c = AssetDatabase.LoadAssetAtPath<BuildConfiguration>(AssetDatabase.GUIDToAssetPath(guid));
                var sceneListComponent = c.GetComponent<SceneList>();
                sceneListComponent.SceneInfos = scenelist;
                c.SetComponent<SceneList>(sceneListComponent);
                c.SaveAsset();
            }
            AssetDatabase.Refresh();
            Log("*************** SetupGraphicsTestCases - Synced "+buildSettingScenes.Length+ " scenes to scenelist on "+assets.Length+" Assets/Tests/Editor/ BuildConfig assets.");
        }
        else
        {
            //Yamato will run this
            var sceneListComponent = config.GetComponent<SceneList>();
            sceneListComponent.SceneInfos = scenelist;
            config.SetComponent<SceneList>(sceneListComponent);
            Log("*************** SetupGraphicsTestCases - Synced "+buildSettingScenes.Length+ " scenes to scenelist");
        }       
    }

    [MenuItem("GraphicsTest/Debug/CreateFolder")]
    public static void CreateFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets\\StreamingAssets"))
        {
            AssetDatabase.CreateFolder("Assets", "StreamingAssets");
            AssetDatabase.Refresh();
        }

        if (!AssetDatabase.IsValidFolder("Assets\\StreamingAssets\\SubScenes"))
        {
            AssetDatabase.CreateFolder("Assets\\StreamingAssets", "SubScenes");
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("GraphicsTest/Debug/CopyFiles")]
    public static void CopyFiles()
    {
        string projPath = Application.dataPath.Replace("/Assets", "");
        string srcPath = "";
        string dstPath = projPath + "/Assets/StreamingAssets/SubScenes";

        //decide path
        switch (target)
        {
            case BuildTarget.StandaloneWindows: srcPath = projPath + "/Builds/GraphicsTest/GraphicsTest_Data/StreamingAssets/SubScenes"; break;
            case BuildTarget.StandaloneWindows64: srcPath = projPath + "/Builds/GraphicsTest/GraphicsTest_Data/StreamingAssets/SubScenes"; break;
            case BuildTarget.StandaloneOSX: srcPath = projPath + "/Builds/GraphicsTest/GraphicsTest.app/Contents/Resources/Data/StreamingAssets/SubScenes"; break;
        }

        //Windows
        //srcPath = @"D:\UnityProject\dots\HybridURPSamples\Builds\HybridURPSamplesBuildSettings\HybridURPSamples_Data\StreamingAssets\SubScenes";
        //dstPath = @"D:\UnityProject\dots\HybridURPSamples\Assets\StreamingAssets\SubScenes";

        Log("*************** SetupGraphicsTestCases - srcPath = " + srcPath);
        Log("*************** SetupGraphicsTestCases - dstPath = " + dstPath);

        string[] fileList = Directory.GetFiles(srcPath, "*.*", SearchOption.AllDirectories);
        for (int i = 0; i < fileList.Length; i++)
        {
            File.Copy(fileList[i], fileList[i].Replace(srcPath, dstPath), true);
        }

        AssetDatabase.Refresh();
        Log("*************** SetupGraphicsTestCases - CopyFile Done. You can now do Testrunner > Run All in player");
    }
}