# HelloNetcode RPC sample

Remote Procedure Calls (RPC) means executing a function on a remote endpoint. In the netcode package this involves setting up a payload component and sending that as a message to client/server connections where they can be processed/handled.

See

* [RPCs](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/rpcs.html)

## Requirements

Only needs an established connection to send to.

* Connection

## Sample description

This sample is meant to show each type of RPC message you can send with the netcode package on clients and servers. It has a GameObject based UI which presents a simple chat window to demonstrate simple RPC routines as chat and user messages are sent:

* When a client connects the server RPC broadcasts that client as a new user to existing connections (including the new clients connection).
* The existing user list is also sent to just that new connection only (target RPC).
* Clients sends chat messages just to the server (uses broadcast type as target can only be the server).
* When the server receives chat messages he RPC broadcasts it to all connections, including the sender which will then show his own message in the chat window.
