# HelloNetcode Relay Server sample

This sample showcase relay server integration.

See

* [Relay server documentation](https://docs.unity.com/relay/introduction.html)
* [Relay server product page](https://unity.com/products/relay)

## Requirements

* Setting up relay support for the project. Follow the description on '**Get started with Relay**' by following the documentation link above.

## Sample description

This sample is connected to the check button in the Frontend sample scene. By toggling this button, the network setup will utilize the relay server service to allow clients to connect to servers hosted via the relay server service.

After enabling **Relay Support**, clicking **Start Client & Server** will start a server with relay support enabled. Then a client will connect to the hosted server through the relay service.
**Join Existing Game** is for connecting a client to a server 

A status message can be seen next to the Relay Server checkbox. If any errors are found, during startup and initialization of the relay service, a failed message will appear and the console log will contain the details.

When toggling relay support, the address and port input field is replaced with a single input field. Here you can enter a join code which is the information needed to connect with a hosted server through the relay service.
Inside the NetCodeSetup/RelayHUD.cs is the entry for the checkbox behaviour. The join code will be visible in the top left corner of the hosted game.

> ## Note
> In play mode you might notice the sample scene taking longer to appear than without the relay support enabled. This delay is caused by the roundtrip time to the relay server.

## Design decisions
This sample enable to choose two different options to test the relay connection:
1. self-hosting using relay for in-going connection, but use IPC to connect the client to the local server connection.
2. emulate a "client" connecting to a remote server, by using relay to connect client to the local server connection. 

The Frontend menu has a specific toogle to select the desired modality.

### Web build constraints

When running the sample using Web build, the start client/server button is disable until the use of relay is checked. 

In `NetcodeSetup/RelayHUD.cs` the client connection endpoint is set to the relay server, which will have to change as described there for the local client. 
