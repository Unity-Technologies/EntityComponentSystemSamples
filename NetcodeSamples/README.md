### Unity Netcode for Entities Samples

*For more Netcode and DOTS starter material, see the [main page of this repo](../README.md).*

The Netcode for Entities package provides the multiplayer features needed to implement
world synchronization in an [entities]((https://docs.unity3d.com/Packages/com.unity.entities@latest))-based multiplayer game. It uses the transport package
for the socket level functionality, Unity Physics for networked physics simulation, Logging package for packet dump logs. Key Netcode for Entities features include:

* Server authoritative synchronization model.
* RPC support, useful for control flow or network events.
* Client / server world bootstrapping so you have clear separation of logic and you can run a server with multiple clients in a single process, like the editor when testing.
* Synchronize entities with interpolation and client side prediction working by default.
* Network traffic debugging tools.
* GameObject conversion flow support, so you can use a hybrid model to add multiplayer to a GameObject/MonoBehaviour based project.

[Netcode for Entities Manual](https://docs.unity3d.com/Packages/com.unity.netcode@latest)

[The Netcode for Entitiets forum](https://forum.unity.com/forums/dots-netcode.425/)

### Samples

#### NetCube
A small sample featuring the Netcode for Entities Package basic features, this is the sample used in the __Getting Started__ guide in the manual.

#### HelloNetcode
This is a suite of samples which aim to be small, simple and show features in isolation. Later samples then re-use earlier ones so it's simpler to see exactly what's being shown and similar routines (like connecting, going in game etc) don't need to be repeated. This way samples can also build on top of each other and become more complex. They are split into Basic, Intermediate and Advanced areas depending on level of complexity and how commonly the things shown are needed in a normal project.

#### Asteroids
A small game featuring the Netcode for Entities Package features.

#### PredictionSwitching
A sample using predicted physics based on Unity Physics. The sample is predicting all objects close the the player but not objects far away. The color of the spheres will change to indicate if they are predicted or interpolated.

#### PlayerList
A sample which shows how to maintain a list of connected players by using the RPC feature.