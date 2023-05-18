# HelloNetcode Bootstrap and Frontend sample

The netcode package provides a way to automatically create client and server worlds and connect them together (client connects to server) by using a custom bootstrapper and the auto connector feature. This works in simple cases when testing out features quickly.

It's also possible to do everything manually via helper methods, including creating the worlds and setting up connections.

When any HelloNetcode sample is directly opened (besides the _Frontend_ scene), the autoconnect functionality is used.

See

* _Establish a connection_ section in the [Getting Started](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/getting-started.html) guide
* [Client Server Worlds](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/client-server-worlds.html)
* [Network Connection](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/network-connection.html)

## Sample description

This sample shows the modification needed to the bootstrap functionality to enable the auto connection feature and to select an alternative manual connection and world creation method, called _Frontend_ here. It's split into two parts:

* Bootstrap folder of the sample has the custom bootstrap code file, it enables auto connection when the frontend is not being used and also creates the default client and server worlds (like would happen with the default netcode bootstrap).
* Frontend folder shows a UI for selecting at runtime if you'd want to host a game or join one. It creates at runtime either a only a client world (join) or both client and server (host) and then manually starts listening or connecting as appropriate.

The frontend is placed in a separate assembly from the bootstrap assembly since this is something we do not want to include in dedicated server builds (UNITY_SERVER is defined).

There three scenes here:

* _Frontend.unity_ has manual connection setup (connect or host) and on demand world creation (at runtime) with scene selection of all sample scenes in the project.
* There is a companion _FrontendHUD.unity_ scene which shows the UI for going back to the main menu after a scene has been opened from the frontend, and text for the connection status.
* A _FrontendBootstrap_ scene is meant to be placed first in the build settings scene list and sets up a few flows for the first sample scene loaded. It loads the Asteroids sample by default when a dedicated server is launched, but otherwise the frontend scene. When a scene name is given on the command line it picks that instead.

## Note

> [!NOTE]
> The Frontend scene contains a toggle for relay support. To read more about what this feature entails, refer to the 01b_RelaySupport description.