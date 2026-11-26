using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    // One per sample scene. Always compiled, so it is still here when the rest of the sample is compiled out. The
    // sample starts and spawns only while Passed is true; otherwise this shows what to fix.
    [DefaultExecutionOrder(-2000)]
    public class StartChecks : MonoBehaviour
    {
        const int k_WindowId = 0x2D2E;

        static StartChecks s_Instance;

        public static bool Passed => s_Instance != null && IsCompiledIn() && IsWorldSetupSupported();

        static bool IsExperimentalDefineEnabled()
        {
#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
            return true;
#else
            return false;
#endif
        }

        static bool IsPhysicsCore2DModuleEnabled()
        {
#if GAMEOBJECTSAMPLE_MODULE_PHYSICSCORE2D
            return true;
#else
            return false;
#endif
        }

        // GameObject ghosts aren't supported with a client and server world in one process, the same as 3D physics:
        // both worlds' GameObjects share the scene, so every ghost renders twice.
        static bool IsWorldSetupSupported() =>
            NetcodeConfig.Global == null ||
            NetcodeConfig.Global.HostWorldModeSelection != NetcodeConfig.HostWorldMode.BinaryWorlds ||
            ClientServerBootstrap.ServerWorld == null ||
            ClientServerBootstrap.ClientWorld == null;

        // Only the first failing check, in order, so the player fixes one thing at a time.
        static string BuildMessage()
        {
            if (!IsExperimentalDefineEnabled())
            {
                return "2D Physics suport is currently experimental. Enable it by adding:\n\n" +
                    "'NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL'\n\n"+
                    "scripting define symbol in:\n\n" +
                    "Edit > Project Settings > Player > Script Compilation > Scripting Define Symbols";
            }
            if (!IsPhysicsCore2DModuleEnabled())
                return "Enable the PhysicsCore2D built-in package (com.unity.modules.physicscore2d) in the Package Manager.";
            return "The netcode config's Host World Mode is Binary Worlds, so the client and server run as " +
                "separate worlds in this process. GameObject ghosts don't support that yet.\n\n" +
                "Set 'Host World Mode Selection' to 'Single World' inside 'NetcodeConfig' in the Samples 'Assets' " +
                "folder in the Project view, and use a Multiplayer Play Mode virtual player as the client.";
        }

        static bool IsCompiledIn() => IsExperimentalDefineEnabled() && IsPhysicsCore2DModuleEnabled();

        void Awake() => s_Instance = this;

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        void OnGUI()
        {
            if (Passed)
                return;
            var size = new Vector2(480, 260);
            var rect = new Rect((Screen.width - size.x) * 0.5f, (Screen.height - size.y) * 0.5f, size.x, size.y);
            GUILayout.Window(k_WindowId, rect, DrawWindow, "2D Physics Sample Can't Start");
        }

        static void DrawWindow(int id) => GUILayout.Label(BuildMessage(), new GUIStyle(GUI.skin.label) { wordWrap = true });
    }
}
