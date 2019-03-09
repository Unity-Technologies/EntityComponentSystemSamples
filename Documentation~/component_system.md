# ComponentSystem

A `ComponentSystem` in Unity (also known as a system in standard ECS terms) performs operations on [entities](entity.md). A `ComponentSystem` cannot contain instance data. To put this in terms of the old Unity system, this is somewhat similar to an old [Component](https://docs.unity3d.com/Manual/Components.html) class, but one that **only contains methods**. One `ComponentSystem` is responsible for updating all Entities with a matching set of components (that is defined within a struct called a [ComponentGroup](component_group.md)).

Unity ECS provides an abstract class called `ComponentSystem` that you can extend in your code.

See file: _/Packages/com.unity.entities/Unity.Entities/ComponentSystem.cs_.

See also: [System update order](system_update_order.md).



[Back to Unity Data-Oriented reference](reference.md)