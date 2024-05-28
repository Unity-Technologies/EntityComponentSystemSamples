#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER && (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
using UnityEditor.OSXStandalone;
#endif

class Initialization
{
    [InitializeOnLoadMethod]
    static void SetupProject()
    {
#if UNITY_2020_2_OR_NEWER && UNITY_EDITOR_OSX
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX)
            UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64;
#endif
    }

    [InitializeOnLoadMethod]
    static void AutoImportUnityPhysicsSamples()
    {
        if (Application.isPlaying)
        {
            // don't modify the project when entering playmode
            return;
        }
        // else:

        // Find Unity Physics package samples and auto-import them if not yet imported.
        // Note: we are setting packageVersion to null here to ignore the version of the package.
        foreach (var sample in Sample.FindByPackage("com.unity.physics", null))
        {
            if (!sample.isImported)
            {
                //sample.importPath = Application.dataPath + "/Authoring";
                var success = sample.Import(Sample.ImportOptions.HideImportWindow |  Sample.ImportOptions.OverridePreviousImports);
                if (!success)
                {
                    Debug.LogWarning("Failed to import Unity Physics package samples");
                }
            }
        }
    }
}
#endif
