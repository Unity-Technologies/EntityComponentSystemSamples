# HelloNetcode Connection Approval sample

It's possible to validate that a connection is allowed to connect to a server, before treating it as fully connected, to support common scenarios (like backend user authentication, password protected matches etc). The connection will not be assigned a `NetworkId` by the server until the it has been approved by said server, via the 'approval flow' described in this samples code. It's only possible to send `IApprovalRpcCommand` type RPCs during the `Approval` connection phase (see [ConnectionState.State.Approval](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/api/Unity.NetCode.ConnectionState.State.html)).

See

* The _Connnection Approval_ section in [Network Connection](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/network-connection.html)

## Requirements

The _ConnectionApproval_ scene needs to be loaded via the _Frontend_ menu, as it will enable the connection approval flow before starting the server, by setting `NetworkStreamDriver.RequireConnectionApproval` to true. By default, this flow is disabled, and the server will by default immediately approve all client connections.

* BootstrapAndFrontend

An optional requirement uses the [Unity Authentication](https://docs.unity.com/ugs/manual/authentication/manual/overview) service to validate a player account. See the [Get Started](https://docs.unity.com/ugs/en-us/manual/authentication/manual/get-started) guide for details on how to set that up.

## Sample description

This sample demonstrates how to set up a connection approval system for sending and processing `IApprovalRpcCommand` RPCs. Two approval methods are possible in this sample, one with a dummy string and the other using the [Unity Authentication](https://docs.unity.com/ugs/manual/authentication/manual/overview) service. You can change which type is used by toggling it in `ClientConnectionApprovalSystem.OnCreate()`. 

With the first method, the client sends a dummy payload (just an "ABC" string) which the server validates, and then uses to approve the client connection. Debug strings are printed to signal each stage of the process (client sends approval RPC, server processes it).

The second methods demonstrates how a player account could be validated before a player is allowed to join a game session, by using the Authenticaion service. It uses the anonymous login method on the client and sends the given player ID and Access Token to the server for validation. It's validated by fetching the player information for the given ID/token pair from the service.