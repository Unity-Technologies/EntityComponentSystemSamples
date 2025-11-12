using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ContentManagement.Sample.Editor
{
   public class BuildPostprocessorFilteringDuplicates : IPostprocessBuildWithReport 
{
    // Callback order: lower number = earlier call
    public int callbackOrder => 1;

    public void OnPostprocessBuild(BuildReport report)
    {
        var settings = AssetDatabase.LoadAssetAtPath<WeakSceneListScriptableObject>("Assets/2. WeakSceneLoading/Content Settings/WeakSceneList.asset");
        var isTargetingRemote = (settings.ContentSource & ContentSourcePath.Remote) != 0;
        
        // When building for remote content delivery, 
        // it's recommended to remove any redundant assets from StreamingAssets after the build completes.
        // This prevents unnecessary duplication and reduces the final build size.
        // If the build is intended to use only local assets, this step is not needed, 
        // as all required content must remain in StreamingAssets (folder inside of the build).
        if (!isTargetingRemote)
            return;
        
        string pathToBuiltProject = report.summary.outputPath;
        Debug.Log("Build completed: " + pathToBuiltProject);
        
        string streamingAssetsPath = null;

        if (report.summary.platform == BuildTarget.StandaloneWindows || report.summary.platform == BuildTarget.StandaloneWindows64) 
        {
            string buildFolder = Path.GetDirectoryName(pathToBuiltProject);
            string dataFolder = Path.Combine(buildFolder, Path.GetFileNameWithoutExtension(pathToBuiltProject) + "_Data");
            streamingAssetsPath = Path.Combine(dataFolder, "StreamingAssets");
        }
        else if (report.summary.platform == BuildTarget.StandaloneOSX) {
            string buildFolder = pathToBuiltProject; // .app
            string dataFolder = Path.Combine(buildFolder, "Contents", "Resources", "Data");
            streamingAssetsPath = Path.Combine(dataFolder, "StreamingAssets");
        }

        if (streamingAssetsPath != null) 
        {
            // In order to get the DebugCatalog.txt file enable,
            // Please add ENABLE_CONTENT_BUILD_DIAGNOSTICS to "Scripting define" in the Build Profile 
            var path = Directory.GetParent(Application.dataPath).FullName;
            var catalogPath = Path.Combine(path, "Catalog/DebugCatalog.txt");

            if (Directory.Exists(streamingAssetsPath))
            {
                if (File.Exists(catalogPath))
                {
                    // Read the entire catalog content into one string for searching
                    string catalogContent = File.ReadAllText(catalogPath);

                    // Get all files in streamingAssetsPath (non-recursive)
                    var files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
                    int deleteCount = 0;

                    foreach (var filePath in files)
                    {
                        string fileName = Path.GetFileName(filePath);
                        Debug.Log($"<color=yellow>FileName: {fileName}</color> ");
                        if(fileName.Contains(".bin") || fileName.Contains(".txt"))
                            continue;
                        
                        if (catalogContent.Contains(fileName))
                        {
                            File.Delete(filePath);
                            deleteCount++;
                            Debug.Log($"Deleted: {fileName}");
                        }
                    }

                    Debug.Log($"<color=yellow>Deleted {deleteCount} file(s) from</color>: {streamingAssetsPath}");
                }
                else
                {
                    Debug.LogWarning($"Catalog file not found at path: {catalogPath}");
                }
            }
            else
            {
                Debug.LogWarning($"No folder {streamingAssetsPath} found");
            }
        }
    }
} 
}
