# Welcome

Welcome to the Unity Netcode for Entities samples repository!

Here you can find all the resources you need to start prototyping
your own real-time multiplayer games.

[Netcode for Entities Manual](https://docs.unity3d.com/Packages/com.unity.netcode@latest)

[Click here to visit the forum](https://forum.unity.com/forums/dots-netcode.425/)

### Unity Netcode for Entities Package
The netcode for entities package provides the multiplayer features needed to implement
world synchronization in a multiplayer game. It uses the transport package
for the socket level functionality, Unity Physics for networked physics simulation, Logging package for packet dump logs and is made for the [Entity Component System](https://docs.unity3d.com/Packages/com.unity.entities@latest).
Some higher level things it provides are

* Server authoritative synchronization model.
* RPC support, useful for control flow or network events.
* Client / server world bootstrapping so you have clear separation of logic and you can run a server with multiple clients in a single process, like the editor when testing.
* Synchronize entities with interpolation and client side prediction working by default.
* Network traffic debugging tools.
* GameObject conversion flow support, so you can use a hybrid model to add multiplayer to a GameObject/MonoBehaviour based project.

For more information about the netcode package, please see the [Netcode for Entities Documentation](https://docs.unity3d.com/Packages/com.unity.netcode@latest)

### Samples

#### Asteroids
A small game featuring the Netcode for Entities Package features.

#### NetCube
A small sample featuring the Netcode for Entities Package basic features, this is the sample used in the __Getting Started__ guide in the manual.

#### PredictionSwitching
A sample using predicted physics based on Unity Physics. The sample is predicting all objects close the the player but not objects far away. The color of the spheres will change to indicate if they are predicted or interpolated.

#### PlayerList
A sample which shows how to maintain a list of connected players by using the RPC feature.

#### HelloNetcode
This is a suite of samples which aim to be small, simple and show features in isolation. Later samples then re-use earlier ones so it's simpler to see exactly what's being shown and similar routines (like connecting, going in game etc) don't need to be repeated. This way samples can also build on top of each other and become more complex. They are split into Basic, Intermediate and Advanced areas depending on level of complexity and how commonly the things shown are needed in a normal project.

## Installation

To try out samples in this repository all you need to do is open `NetcodeSamples/` in Unity.

If you wish to create a new Unity project using the Netcode package that is also possible.

* Minimum supported version is Unity 2022.2.0f1 but it’s recommended to use the latest released 2022.2 version for important fixes
* Create a new **URP** Unity project
* Navigate to the **Package Manager** (Window -> Package Manager). And add the following packages using **Add package from git URL...** under the **+** menu at the top left of the Package Manager.
  * com.unity.netcode
  * com.unity.entities.graphics
* Package dependencies will automatically be pulled into the project

## Building

Building is done via the **Build Settings** window as with normal Unity builds. Make sure you have the appropriate player type set int he _Entities->Build_ tab in the **Player Settings**. To build the whole sample scene list (with frontend) as a client/server build select the **ClientAndServer** as the *Netcode client target*. To make a client only build select **Client**. For a server only build switch to the **Dedicated Server** platform target. This will automatically use the **Server** configuration.
