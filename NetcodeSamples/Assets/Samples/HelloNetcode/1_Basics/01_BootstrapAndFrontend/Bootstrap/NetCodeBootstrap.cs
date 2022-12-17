using System;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    // This is a setup for dealing with a frontend menu which manually creates client and server worlds,
    // while still allowing play mode on a single level with auto connect.
    // If you do not need a frontend menu and just want to always auto connect it is usually enough to use
    // a simpler bootstrap like this:
    // [UnityEngine.Scripting.Preserve]
    // public class NetCodeBootstrap : ClientServerBootstrap
    // {
    //     public override bool Initialize(string defaultWorldName)
    //     {
    //         AutoConnectPort = 7979; // Enable auto connect
    //         return base.Initialize(defaultWorldName); // Use the regular bootstrap
    //     }
    // }

    // The preserve attibute is required to make sure the bootstrap is not stripped in il2cpp builds with stripping enabled
    [UnityEngine.Scripting.Preserve]
    // The bootstrap needs to extend `ClientServerBootstrap`, there can only be one class extending it in the project
    public class NetCodeBootstrap : ClientServerBootstrap
    {
        // The initialize method is what entities calls to create the default worlds
        public override bool Initialize(string defaultWorldName)
        {
#if UNITY_EDITOR
            // If we are in the editor, we check if the loaded scene is "Frontend",
            // if we are in a player we assume it is in the frontend if FRONTEND_PLAYER_BUILD
            // is set, otherwise we assume it is a single level.
            // The define FRONTEND_PLAYER_BUILD needs to be set in the build config for the frontend player.
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isFrontend = sceneName == "Frontend";
#elif !FRONTEND_PLAYER_BUILD
            bool isFrontend = false;
#endif

#if UNITY_EDITOR || !FRONTEND_PLAYER_BUILD
            if (!isFrontend)
            {
                // This will enable auto connect. We only enable auto connect if we are not going through frontend.
                // The frontend will parse and validate the address before connecting manually.
                // Using this auto connect feature will deal with the client only connect address from Multiplayer PlayMode Tools
                AutoConnectPort = 7979;

                // Use "-port 8000" when running a build from commandline to specify the port to use
                // Will override the default port
                string commandPort = CommandLineUtils.GetCommandLineValueFromKey("port");
                if (!string.IsNullOrEmpty(commandPort))
                    AutoConnectPort = UInt16.Parse(commandPort);


                // Create the default client and server worlds, depending on build type in a player or the Multiplayer PlayMode Tools in the editor
                CreateDefaultClientServerWorlds();
            }
            else
            {
                // Disable the autoconnect in the frontend. The reset i necessary in the Editor since we can start the demos directly and
                // (the AutoConnectPort will lose its default value)
                AutoConnectPort = 0;
                CreateLocalWorld(defaultWorldName);
            }
#else
            CreateLocalWorld(defaultWorldName);
#endif
            return true;
        }
    }
}
