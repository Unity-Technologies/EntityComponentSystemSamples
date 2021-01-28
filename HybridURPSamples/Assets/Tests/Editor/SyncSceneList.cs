using System.Collections;
using System.Collections.Generic;
using Unity.Build;
using Unity.Build.Common;
using UnityEditor;
using UnityEngine;

public class SyncSceneList
{
    //Sync scenelist from BuildSettings to BuildConfig
    [MenuItem("Assets/SyncSceneListToBuildConfig")]
    static void RunSyncSceneList()
    {
        //Get selected object on ProjectView
        var selected = Selection.activeObject;
        if(selected.GetType() != typeof(BuildConfiguration))
        {
            Debug.LogError("Cannot sync scenelist. You've selected "+selected.name+
            " which is not a BuildConfiguration asset. Please select a BuildConfiguration asset on ProjectView and try again.");
            return;
        }

        //Get Buildconfig asset
        BuildConfiguration config = (BuildConfiguration) selected;

        //Get scenelist from BuildSettings
        EditorBuildSettingsScene[] buildSettingScenes = EditorBuildSettings.scenes;
        List<SceneList.SceneInfo> scenelist = new List<SceneList.SceneInfo>();
        for(int i=0;i<buildSettingScenes.Length;i++)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildSettingScenes[i].path);
            scenelist.Add(new SceneList.SceneInfo() { AutoLoad = false, Scene = GlobalObjectId.GetGlobalObjectIdSlow(sceneAsset) });
        }

        //Set the scenelist on Buildconfig asset
        var sceneListComponent = config.GetComponent<SceneList>();
        sceneListComponent.SceneInfos = scenelist;
        config.SetComponent<SceneList>(sceneListComponent);
        config.SaveAsset();
        AssetDatabase.Refresh();

        //Log
        Debug.Log("Successfully synced "+buildSettingScenes.Length+ " scenes to scenelist on "+AssetDatabase.GetAssetPath(config)+" asset.");  
    }
}
