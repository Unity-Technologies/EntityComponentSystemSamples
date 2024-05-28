#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;

class TestsMenu : EditorWindow
{
    const string kE2ETestsMenuItem = "Physics Tests/Enable End To End Determinism Tests";
    const string kE2EDefine = "UNITY_PHYSICS_INCLUDE_END2END_TESTS";

    [MenuItem(kE2ETestsMenuItem, false)]
    static void SwitchUseE2ETests()
    {
        if (DefineExists(kE2EDefine, BuildTargetGroup.Standalone))
        {
            RemoveDefineIfNecessary(kE2EDefine, BuildTargetGroup.Standalone);
        }
        else
        {
            AddDefineIfNecessary(kE2EDefine, BuildTargetGroup.Standalone);
        }
    }

    [MenuItem(kE2ETestsMenuItem, true)]
    static bool SwitchUseE2ETestsValidate()
    {
        Menu.SetChecked(kE2ETestsMenuItem, DefineExists(kE2EDefine, BuildTargetGroup.Standalone));

        return true;
    }

    public static bool DefineExists(string _define, BuildTargetGroup _buildTargetGroup)
    {
        var fromBuildTargetGroup = NamedBuildTarget.FromBuildTargetGroup(_buildTargetGroup);
        var defines = PlayerSettings.GetScriptingDefineSymbols(fromBuildTargetGroup);

        return defines != null && defines.Length > 0 && defines.IndexOf(_define, 0) >= 0;
    }

    public static void AddDefineIfNecessary(string _define, BuildTargetGroup _buildTargetGroup)
    {
        var fromBuildTargetGroup = NamedBuildTarget.FromBuildTargetGroup(_buildTargetGroup);
        var defines = PlayerSettings.GetScriptingDefineSymbols(fromBuildTargetGroup);

        if (defines == null) { defines = _define; }
        else if (defines.Length == 0) { defines = _define; }
        else { if (defines.IndexOf(_define, 0) < 0) { defines += ";" + _define; } }

        PlayerSettings.SetScriptingDefineSymbols(fromBuildTargetGroup, defines);
    }

    public static void RemoveDefineIfNecessary(string _define, BuildTargetGroup _buildTargetGroup)
    {
        var fromBuildTargetGroup = NamedBuildTarget.FromBuildTargetGroup(_buildTargetGroup);
        var defines = PlayerSettings.GetScriptingDefineSymbols(fromBuildTargetGroup);

        if (defines.StartsWith(_define + ";"))
        {
            // First of multiple defines.
            defines = defines.Remove(0, _define.Length + 1);
        }
        else if (defines.StartsWith(_define))
        {
            // The only define.
            defines = defines.Remove(0, _define.Length);
        }
        else if (defines.EndsWith(";" + _define))
        {
            // Last of multiple defines.
            defines = defines.Remove(defines.Length - _define.Length - 1, _define.Length + 1);
        }
        else
        {
            // Somewhere in the middle or not defined.
            var index = defines.IndexOf(_define, 0, StringComparison.Ordinal);
            if (index >= 0) { defines = defines.Remove(index, _define.Length + 1); }
        }

        PlayerSettings.SetScriptingDefineSymbols(fromBuildTargetGroup, defines);
    }
}
#endif
