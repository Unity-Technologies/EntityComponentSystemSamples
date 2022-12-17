# HelloNetcode Physics sample

Physics enabled ghosts can be synchronized with some adjustments needed as compared to a normal transform driven ghost. In this sample physics is running on both client and server. Everything on the client is kinematic and driven by the server, but the player is predicted so gets physics simulated immediately and corrected by server snapshot updates. This is using the Unity Physics (com.unity.physics) package.

See

* [Physics](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/physics.html)
* [Unity Physics](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/TableOfContents.html) package

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when connection is established.

* GoInGame
* SpawnPlayer

## Sample description

To make it possible to simulate predicted ghosts on both the client and server, and only interpolated ghosts on the server, the _NetCodePhysicsConfig_ component needs to be added to the project. The _Predict physics_ checkbox needs to be checked but the default values will work fine here.

The physics player prefab has been modified compared to the spawn player sample like so:

* Add _PhysicsBody_ and _PhysicsShape_ components
* Change _Shape Type_ to _Capsule_ to match player shape.
* Change _Motion Type_ to _Kinematic_ so player is manually driven.
* Ghost authoring is unchanged (owner predicted)

Since it's kinematic, it is manually controlled instead of being affected by physics (like gravity). But it can interact with other physics objects.

The interpolated Barrel prefab has the same physics settings except

* _Motion Type_ is dynamic (the default value).
* Change _Shape Type_ is _Cylinder_ to match it's shape
* _Supported Ghost Mode_ is set to _Interpolated_

It is purely driven by the server simulation, the client just applies the values given by the server.

The predicted Barrel prefab has the same physics components set up like the interpolated one, but 

* Ghost authoring component has _Supported Ghost Mode_ set to _All_ and _Default Ghost Mode_ set to _Predicted_.

In both cases (interpolated and predicted) the barrels will be set to kinematic on the client but predicted ones will have the simulation step manually driven from the physics prediction group, so will be affected by physics forces like gravity.

## Notes

Two things can affect how it looks when the predicted player runs into the interpolated barrels. Firstly, if the framerate dips below 60 here the physics simulation will start to suffer and look worse. This can be improved by ensuring you have

* Burst compilation enabled
* Jobs Debugger disabled

Secondly with more latency it can become necessary to predict further back in time when applying new ghost snapshot updates. This adds a performance penalty as the ghost prediction system groups needs to run multiple times. This can be tested via the simulation parameter in the playmode tools and having a look at the timeline in the cpu module in the profiling window. Add for example 50 ms latency and see difference in the number of times the _PredictedPhysicsSystemsGroup_ executes inside _GhostPredictionSystemGroup_.
