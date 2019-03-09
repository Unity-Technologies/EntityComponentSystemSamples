# SystemStateComponents

The purpose of `SystemStateComponentData` is to allow you to track resources internal to a system and have the opportunity to appropriately create and destroy those resources as needed without relying on individual callbacks.

`SystemStateComponentData` and `SystemStateSharedComponentData` are exactly like `ComponentData` and `SharedComponentData`, respectively, except in one important respect:

1. `SystemStateComponentData` is not deleted when an `Entity` is destroyed.

`DestroyEntity` is shorthand for

1. Find all components which reference this particular `Entity` ID.
2. Delete those components.
3. Recycle the `Entity` id for reuse.

However, if `SystemStateComponentData` is present, it is not removed. This gives a system the opportunity to cleanup any resources or state associated with an `Entity` ID. The `Entity` ID will only be reused once all `SystemStateComponentData` has been removed.

## Motivation

- Systems may need to keep an internal state based on `ComponentData`. For instance, resources may be allocated. 
- Systems need to be able to manage that state as values and state changes are made by other systems. For example, when values in components change, or when relevant components are added or deleted.
- "No callbacks" is an important element of the ECS design rules.

## Concept

The general use of  `SystemStateComponentData` is expected to mirror a user component, providing the internal state.

For instance, given:
- FooComponent (`ComponentData`, user assigned)
- FooStateComponent (`SystemComponentData`, system assigned)

### Detecting Component Add

When user adds FooComponent, FooStateComponent does not exist. The FooSystem update queries for FooComponent without FooStateComponent and can infer that they have been added. At that point, the FooSystem will add the FooStateComponent and any needed internal state. 

### Detecting Component Remove

When user removes FooComponent, FooStateComponent still exists. The FooSystem update queries for FooStateComponent without FooComponent and can infer that they have been removed. At that point, the FooSystem will remove the FooStateComponent and fix up any needed internal state. 

### Detecting Destroy Entity

`DestroyEntity` is actually a shorthand utility for:

- Find components which reference given `Entity` ID.
- Delete components found.
- Recycle `Entity` ID.

However, `SystemStateComponentData` are not removed on `DestroyEntity` and the `Entity` ID is not recycled until the last component is deleted. This gives the system the opportunity to clean up the internal state in the exact same way as with component removal.

## SystemStateComponent

A `SystemStateComponentData` is analogous to a `ComponentData` and used similarly.

```
struct FooStateComponent : ISystemStateComponentData
{
}
```

Visibility of a `SystemStateComponentData` is also controlled in the same way as a component (using `private`, `public`, `internal`) However, it's expected, as a general rule, that a `SystemStateComponentData` will be `ReadOnly` outside the system that creates it.

## SystemStateSharedComponent

A `SystemStateSharedComponentData` is analogous to a `SharedComponentData` and used similarly.

```
struct FooStateSharedComponent : ISystemStateSharedComponentData
{
  public int Value;
}
```

[Back to Unity Data-Oriented reference](reference.md)
