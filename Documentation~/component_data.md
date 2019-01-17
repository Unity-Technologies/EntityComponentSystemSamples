# ComponentData

ComponentData in Unity (also known as a Component in standard ECS terms) is a struct that contains only the instance data for an [Entity](entity.md). ComponentData cannot contain methods. To put this in terms of the old Unity system, this is somewhat similar to an old Component class, but one that **only contains variables**.

Unity ECS provides an interface called `IComponentData` that you can implement in your code. 

## IComponentData

Traditional Unity components (including `MonoBehaviour`) are [object-oriented](https://en.wikipedia.org/wiki/Object-oriented_programming) classes which contain data and methods for behavior. `IComponentData` is a pure ECS-style component, meaning that it defines no behavior, only data. `IComponentData` is a struct rather than a class, meaning that it is copied [by value instead of by reference](https://stackoverflow.com/questions/373419/whats-the-difference-between-passing-by-reference-vs-passing-by-value?answertab=votes#tab-top) by default. You will usually need to use the following pattern to modify data:

```C#
var transform = group.transform[index]; // Read

transform.heading = playerInput.move; // Modify
transform.position += deltaTime * playerInput.move * settings.playerMoveSpeed;

group.transform[index] = transform; // Write
```

`IComponentData` structs may not contain references to managed objects. Since the all `ComponentData` lives in simple non-garbage-collected tracked [chunk memory](chunk_iteration.md).

See file: _/Packages/com.unity.entities/Unity.Entities/IComponentData.cs_.

[Back to Unity Data-Oriented reference](reference.md)