### Unity Netcode Samples

*For more Netcode and DOTS starter material, see the [main page of this repo](../README.md).*

The Unity Netcode package provides a foundation for creating networked multiplayer applications using both entities and GameObject workflows. It uses the transport package
for the socket level functionality, Unity Physics or GameObject (PhysX) for networked physics simulation. Key Netcode features include:

* Server authoritative synchronization model.
* RPC support, useful for control flow or network events.
* Client / server world bootstrapping so you have clear separation of logic and you can run a server with multiple clients in a single process, like the editor when testing.
* Synchronize entities with interpolation and client side prediction working by default.
* Network traffic debugging tools.

[Unity Netcode Manual](https://docs.unity3d.com/Packages/com.unity.netcode@latest)

[Unity Netcode forum](https://discussions.unity.com/c/multiplayer-and-networking/unity-netcode/156/none)

### Samples

#### GameObjectSample
A GameObject sample showing various netcode features and how to interact with them using GameObject APIs. No ECS needed.

#### NetCube
A small sample featuring the entities facing basic features, this is one of the samples used in the __Getting Started__ guide in the manual.

#### HelloNetcode
This is a suite of samples which aim to be small, simple and show features in isolation. Later samples then re-use earlier ones so it's simpler to see exactly what's being shown and similar routines (like connecting, going in game etc) don't need to be repeated. This way samples can also build on top of each other and become more complex. They are split into Basic, Intermediate and Advanced areas depending on level of complexity and how commonly the things shown are needed in a normal project.

#### Asteroids
A small game featuring the entities facing features.

#### PredictionSwitching
A sample using predicted physics based on Unity Physics. The sample is predicting all objects close the the player but not objects far away. The color of the spheres will change to indicate if they are predicted or interpolated.

#### PlayerList
A sample which shows how to maintain a list of connected players by using the RPC feature.