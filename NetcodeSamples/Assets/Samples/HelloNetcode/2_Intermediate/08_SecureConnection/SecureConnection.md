# HelloNetcode Secure Connection sample

The NetCode package provides a way to create client and server worlds and securely connect them together (client connects to server) by using a custom bootstrapper.

See

* _Establish a connection_ section in the [Getting Started](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/getting-started.html) guide
* [Client Server Worlds](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/client-server-worlds.html)
* [Network Connection](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/network-connection.html)
* [Secure Client and Server](https://docs-multiplayer.unity3d.com/transport/current/secure-connection)
* [Generate Required Keys and Certificate](https://docs-multiplayer.unity3d.com/transport/current/secure-connection#generating-the-required-keys-and-certificates-with-openssl)

## Requirements

Only needs the bootstrap to set-up client and server world.

* BootstrapAndFrontend

## Sample description

This sample shows the modification needed to the bootstrap functionality to enable the secure connection feature.

This sample contains no scene as nothing needs to be added to the scene to enable this feature.

To set up a secure connection on the network driver a custom/manual driver needs to be set up so different network parameters can be passed to it.
The custom driver constructor needs to be set up in the bootstrap to be there early enough to replace the default driver. This is done in _SecureBootStrapExtension.cs_ but it's disabled with the _ENABLE_NETCODE_SAMPLE_SECURE_ define at the top of the file, since enabling it means it's enforced globally through the whole project. To enable it just uncomment the define there as well as in _NetworkParams.cs_ and _SecureDriverConstructor.cs_.

### Generating secure parameters

Inside _NetworkParams.cs_ you will see static variables containing this sample's generated certificate. For your own game you would generate these by yourself. Follow the link '[Generate Required Keys and Certificate]' above to see how to do this.

**NOTE:** Make sure that you do not ship your generated keys and certificates when publishing your game.
It is very easy for a malicious user to decompile the source code even if obfuscated and see the keys.

### Passing secure parameters

The generated certificates are passed to the network driver in _SecureDriverConstructor.cs_. This is using a helper function to set up default values on the network settings.
You can change this to manually construct the network driver instance, or pass in your own network settings using the appropriate override.
