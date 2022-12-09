using UnityEditor;

#if UNITY_2020_2_OR_NEWER && (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
using UnityEditor.OSXStandalone;
#endif

[InitializeOnLoad]
class SetupProject
{
    static SetupProject()
    {
#if UNITY_2020_2_OR_NEWER && UNITY_EDITOR_OSX
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX)
            UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64;
#endif
    }
}
