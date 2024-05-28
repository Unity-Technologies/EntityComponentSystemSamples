#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Build;

[InitializeOnLoad]
public class RemoveDefineOnShutdown
{
    const string k_UnityPhysicsCustom = "UNITY_PHYSICS_CUSTOM";
    const string k_CIEnvVar = "CI";

    static RemoveDefineOnShutdown()
    {
        EditorApplication.quitting += RemoveDefine;
    }

    static void RemoveDefine()
    {
        var ciEnvVarOverride = Environment.GetEnvironmentVariable(k_CIEnvVar);

        if (ciEnvVarOverride is not(null or ""))
        {
            var fromBuildTargetGroup = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone);
            string defineSymbols = PlayerSettings.GetScriptingDefineSymbols(fromBuildTargetGroup);

            defineSymbols = defineSymbols.Replace($";{k_UnityPhysicsCustom}", "");

            PlayerSettings.SetScriptingDefineSymbols(fromBuildTargetGroup, defineSymbols);
        }
    }
}
#endif
