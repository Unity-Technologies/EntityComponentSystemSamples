using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.Entities.Build;
using Unity.Entities.Content;
using System.Collections.Generic;

namespace ContentManagement.Sample.Editor
{
    public class ContentBuilder
    {
        // to publish the content catalog, invoke this function by right-clicking the WeakSceneListScriptableObject
        // instance in the Assets window, then click Publish -> Publish Catalog
        [MenuItem("Assets/Publish/Publish Catalog from a WeakSceneListScriptableObject")]
        private static void PublishContent(MenuCommand command)
        {
            WeakSceneListScriptableObject weakSceneList = Selection.activeObject as WeakSceneListScriptableObject;

            if (weakSceneList == null)
            {
                Debug.LogError("Publish Catalog is only supported for WeakSceneListScriptableObject assets. Please select an existing one or create a new asset.");
                return;
            }
        
            Debug.Log("Publishing content catalog");
        
            // collect the GUIDs of the subscenes we want to include in the catalog
            var subSceneGuids = new HashSet<Unity.Entities.Hash128>();
            foreach (var weakScene in weakSceneList.LocalScenes)
            {
                subSceneGuids.Add(weakScene.Id.GlobalId.AssetGUID);
            }
            foreach (var weakScene in weakSceneList.RemoteScenes)
            {
                subSceneGuids.Add(weakScene.Id.GlobalId.AssetGUID);
            }
        
            var tempPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), $"ContentUpdateBuildDir/{PlayerSettings.productName}");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
        
            // The player guid is used to identify the type of build
            // (when using Netcode for Entities, must distinguish between client and server)
            var playerGuid = (DotsGlobalSettings.Instance.GetPlayerType() == DotsGlobalSettings.PlayerType.Client)
                ? DotsGlobalSettings.Instance.GetClientGUID()
                : DotsGlobalSettings.Instance.GetServerGUID();
            if (!playerGuid.IsValid)
            {
                throw new Exception("Invalid Player GUID");
            }

            Debug.Log($"<color=green>Content catalog will built</color>: {subSceneGuids.Count} subscenes");
        
            // builds the subscenes and stores them in tempPath 
            RemoteContentCatalogBuildUtility.BuildContent(
                subSceneGuids, playerGuid, EditorUserBuildSettings.activeBuildTarget, tempPath);
        
            // copies from tempPath to the target folder and renames the assets to their content hashes.  
            var contentPath = WeakSceneListScriptableObject.ContentPath;
            var contentSetName = WeakSceneListScriptableObject.ContentSetName;
            if (RemoteContentCatalogBuildUtility.PublishContent(tempPath, contentPath, f => new string[] { contentSetName } ))
            {
                Debug.Log($"<color=green>Content catalog published</color>");
                Directory.Delete(tempPath, true);
            }
        }
    }
}