using System;
using System.Collections.Generic;
using System.IO;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Samples.HelloNetcode
{
    // This is a setup for dealing with a frontend menu, where the user wants control over client and server world creation.
    // We support:
    // - Starting a game into the Frontend scene, allowing the user to choose:
    //      - A 'client hosted' setup.
    //      - A 'connect to existing server via IP' setup.
    //      - A 'auto-load scene via `-scene XXX` commandline arg.
    // - While starting from any other scene will preserve the existing 'auto-connect' quick-start flow.

    // If you do not need a frontend menu (and just want to always auto connect), it is usually enough to use
    // a simpler bootstrap, like this:
    // [UnityEngine.Scripting.Preserve]
    // public class NetCodeBootstrap : ClientServerBootstrap
    // {
    //     public override bool Initialize(string defaultWorldName)
    //     {
    //         AutoConnectPort = 7979; // Enable auto connect
    //         return base.Initialize(defaultWorldName); // Use the regular bootstrap
    //     }
    // }

    // The preserve attribute is required to make sure the bootstrap is not stripped in il2cpp builds with stripping enabled.
    [UnityEngine.Scripting.Preserve]
    // The bootstrap needs to extend `ClientServerBootstrap`, there can only be one class extending it in the project.
    public class FrontendBootstrap : ClientServerBootstrap
    {
        // The initialize method is what Entities calls to create the default worlds.
        public override bool Initialize(string defaultWorldName)
        {
            const string fallbackGameplayScene = "Asteroids";
            const string frontendScene = "Frontend";

            // If the user added an OverrideDefaultNetcodeBootstrap MonoBehaviour to their active scene,
            // or disabled Bootstrapping project-wide, we should respect that here.
            if (!DetermineIfBootstrappingEnabled())
                return false;

            // We check if the loaded scene is "Frontend", which means we should DISABLE auto-connect flows.
            // We also check to see if the user has any commandline argument directing which scene we should load.
            var isFromCommandLine = TryGetCommandLineScene(out var targetScene);
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!isFromCommandLine) targetScene = activeScene;

            var isFrontend = targetScene == frontendScene;

            // Handle server setup errors:
            if (IsServerPlatform)
            {
                if (isFrontend)
                {
                    Debug.LogWarning($"[FrontendBootstrap] Server build loaded the isFrontend scene ('{activeScene}'), but cannot run it, so defaulting to {nameof(fallbackGameplayScene)}: '{fallbackGameplayScene}'!");
                    targetScene = fallbackGameplayScene;
                    isFrontend = false;
                }
                else if (isFromCommandLine && string.IsNullOrEmpty(targetScene))
                {
                    Debug.LogError($"[FrontendBootstrap] Server build with invalid commandline scene, so defaulting to {nameof(fallbackGameplayScene)}: '{fallbackGameplayScene}'!");
                    targetScene = fallbackGameplayScene;
                }
            }

            // Handle flow errors:
            if (targetScene == "FrontendHUD")
            {
                targetScene = frontendScene;
                isFrontend = true;
                Debug.LogError($"[FrontendBootstrap] Cannot start via the 'FrontendHUD' scene! Loading {nameof(frontendScene)}: '{frontendScene}' instead!");
            }

            if(!Application.isEditor)
                Debug.Log($"[FrontendBootstrap] startupTime: {Time.realtimeSinceStartupAsDouble:0.0}s, targetScene: '{targetScene}', isFromCommandLine: {isFromCommandLine}, isFrontend: {isFrontend}!");

            if (isFrontend)
            {
                AutoConnectPort = 0; // Disable the auto-connect in the frontend.
                CreateLocalWorld(defaultWorldName); // Don't create the Client & Server worlds,
                                                    // as we do so conditionally (depending on what the user chooses
                                                    // via the FrontendHUD UI).
            }
            else
            {
                // This will enable auto connect. We only enable auto connect if we are not going through frontend.
                // The frontend will parse and validate the address before connecting manually.
                // Using this auto connect feature will deal with the client only connect address from PlayMode Tools
                AutoConnectPort = 7979;

                // Use "-port 8000" when running a build from commandline to specify the port to use
                // Will override the default port
                string commandPort = CommandLineUtils.GetCommandLineValueFromKey("port");
                if (!string.IsNullOrEmpty(commandPort))
                    AutoConnectPort = UInt16.Parse(commandPort);

                // Create the appropriate worlds, which we can then load sub-scenes directly into:
                CreateDefaultClientServerWorlds();

                // We're not in the frontend, so load directly into whatever gameplay scene is chosen by the above bootstrap flow.
                // We may need to change scene here, so do so:
                if (activeScene != targetScene)
                {
                    Debug.Log($"[FrontendBootstrap] {nameof(activeScene)}: '{activeScene}' is not {nameof(targetScene)}: '{targetScene}', so switching to it!");
                    SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
                }
            }
            return true;
        }

        /// <summary>
        /// This is essentially #if UNITY_SERVER, but without having to worry about introducing compiler errors.
        /// </summary>
        private static bool IsServerPlatform => Application.platform == RuntimePlatform.LinuxServer
                                                || Application.platform == RuntimePlatform.WindowsServer
                                                || Application.platform == RuntimePlatform.OSXServer;

        private static bool TryGetCommandLineScene(out string commandLineScene)
        {
            // Commandline always overrides defaults if it exists
            commandLineScene = CommandLineUtils.GetCommandLineValueFromKey("scene");
            if (string.IsNullOrWhiteSpace(commandLineScene))
            {
                commandLineScene = null;
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var scene = Path.GetFileNameWithoutExtension(scenePath);
                if (commandLineScene == scene)
                {
                    return true;
                }
            }

            Debug.LogError($"$TryGetCommandLineScene: '{commandLineScene}' not found. Scenes present in the build\n: {string.Join(',', GetAllScenesInBuild())}");

            static IEnumerable<string> GetAllScenesInBuild()
            {
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
                {
                    var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    yield return Path.GetFileNameWithoutExtension(scenePath);
                }
            }
            return false;
        }
    }
}
