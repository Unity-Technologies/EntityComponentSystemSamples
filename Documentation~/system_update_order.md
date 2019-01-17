## System update order

In ECS all systems are updated on the main thread. The order in which the are updated is based on a set of constraints and an optimization pass which tries to order the system in a way so that the time between scheduling a job and waiting for it is as long as possible.

The attributes to specify update order of systems are ```[UpdateBefore(typeof(OtherSystem))]``` and ```[UpdateAfter(typeof(OtherSystem))]```. In addition to update before or after other ECS systems it is possible to update before or after different phases of the Unity PlayerLoop by using typeof([UnityEngine.Experimental.PlayerLoop.FixedUpdate](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Experimental.PlayerLoop.FixedUpdate.html)) or one of the other phases in the same namespace.

The `UpdateInGroup` attribute will put the system in a group and the same `UpdateBefore` and `UpdateAfter` attributes can be specified on a group or with a group as the target of the before/after dependency.

To use `UpdateInGroup` you need to create and empty class and pass the type of that to the `UpdateInGroup` attribute

```cs
public class UpdateGroup
{}

[UpdateInGroup(typeof(UpdateGroup))]
class MySystem : ComponentSystem
```

[Back to Unity Data-Oriented reference](reference.md)