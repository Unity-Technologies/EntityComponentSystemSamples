# HelloNetcode connection monitor sample

A connection entity will be created as soon as a server starts listening or client starts to connect. It will initially get a `NetworkStreamConnection` component and then others depending on how it's configured. This can be queried to get the status of the connection (see `ConnectionState`).

The connection is set up with a shorter custom disconnect timeout so timeouts can be quickly tested.

See

* _Connection_ section in the [Entities list](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/entities-list.html) shows all the components the network connection entity can have.
* [Network Connection](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/network-connection.html)

## Sample description

This sample shows how you can detect various events by querying for connection components. A connection entity is created with the `ConnectionState` component which reflects the connection flow

* Disconnected
* Connecting
* Handshake
* Connected

To set up a disconnect timeout on the driver a custom/manual driver needs to be set up so different network parameters can be passed to it. The custom driver constructor needs to be set up in the bootstrap to be there early enough to replace the default driver. This is done in _NetCodeBootstrapExtension.cs_ but it's disabled with the _ENABLE_NETCODE_SAMPLE_TIMEOUT_ define at the top of the file, since enabling it means it's enforced globally through the whole project. To enable it just uncomment the define.

## Notes

Try it with one thin client added in the playermode tools to see what messages are passed around when it is disconnected. Try making a standalone build with this sample (can be picked from frontend sample list) and then terminate the standalone process when testing to see the timeout event.

The UI is not set up to do anything fancy but just demonstrate connection events on disconnections.
