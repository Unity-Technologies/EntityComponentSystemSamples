# Netcode for Entities samples

- [Netcode for Entities Manual](https://docs.unity3d.com/Packages/com.unity.netcode@latest)
- [Netcode forums](https://forum.unity.com/forums/dots-netcode.425/)

### Asteroids

A small game featuring the Netcode for Entities Package features.

### NetCube

A small sample featuring the Netcode for Entities Package basic features, this is the sample used in the __Getting Started__ guide in the manual.

### PredictionSwitching

A sample using predicted physics based on Unity Physics. The sample is predicting all objects close the the player but not objects far away. The color of the spheres will change to indicate if they are predicted or interpolated.

### PlayerList

A sample which shows how to maintain a list of connected players by using the RPC feature.

### HelloNetcode

This is a suite of samples which aim to be small, simple and show features in isolation. Later samples then re-use earlier ones so it's simpler to see exactly what's being shown and similar routines (like connecting, going in game etc) don't need to be repeated. This way samples can also build on top of each other and become more complex. They are split into Basic, Intermediate and Advanced areas depending on level of complexity and how commonly the things shown are needed in a normal project.

## Building

Building is done via the **Build Settings** window as with normal Unity builds. Make sure you have the appropriate player type set int he _Entities->Build_ tab in the **Player Settings**. To build the whole sample scene list (with frontend) as a client/server build select the **ClientAndServer** as the *Netcode client target*. To make a client only build select **Client**. For a server only build switch to the **Dedicated Server** platform target. This will automatically use the **Server** configuration.