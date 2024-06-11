#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Unity.Physics.Authoring
{
    [InitializeOnLoad]
    class EditorInitialization
    {
        static readonly string k_CustomDefine = "UNITY_PHYSICS_CUSTOM";

        static EditorInitialization()
        {
            var fromBuildTargetGroup = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var definesStr = PlayerSettings.GetScriptingDefineSymbols(fromBuildTargetGroup);
            var defines = definesStr.Split(';').ToList();
            var found = defines.Find(define => define.Equals(k_CustomDefine));
            if (found == null)
            {
                defines.Add(k_CustomDefine);
                PlayerSettings.SetScriptingDefineSymbols(fromBuildTargetGroup, string.Join(";", defines.ToArray()));
            }
        }
    }
}
#endif
