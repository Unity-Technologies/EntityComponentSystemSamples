using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEditor;
using System.Diagnostics;
using UnityEditor.Build.Content;
#if UNITY_2020_2_OR_NEWER && (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
using UnityEditor.OSXStandalone;
#endif

[InitializeOnLoad]
public class SetupProject
{
    static SetupProject()
    {
#if UNITY_2020_2_OR_NEWER && UNITY_EDITOR_OSX
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX)
            UserBuildSettings.architecture = MacOSArchitecture.x64;
#endif
    }
}
