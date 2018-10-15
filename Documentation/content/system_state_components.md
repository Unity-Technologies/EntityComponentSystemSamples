# SystemStateComponents

## Motivation

- Systems may need to keep an internal state based on component data. For instance, resources may be allocated. 
- Systems need to be able to manage that state as values and state changes are made by other systems. For example, when values in components change, or when relevant components are added or deleted.
- "No callbacks" is an important element of the ECS design rules.

## Concept

The general use of a [SystemStateComponent](system_state_components.md) is expected to mirror a user component, providing the internal state.

For instance, given:
- FooComponent (`ComponentData`, user assigned)
- FooStateComponent (`SystemComponentData`, system assigned)

### Detecting Component Add

When user adds FooComponent, FooStateComponent does not exist. The FooSystem update queries for FooComponent without FooStateComponent and can infer that they have been added. At that point, the FooSystem will add the FooStateComponent and any needed internal state. 

### Detecting Component Remove

When user removes FooComponent, FooStateComponent still exists. The FooSystem update queries for FooStateComponent without FooComponent and can infer that they have been removed. At that point, the FooSystem will remove the FooStateComponent and fix up any needed internal state. 

### Detecting Destroy Entity

`DestroyEntity` is actually a shorthand utility for:
- Find Components which reference given Entity ID.
- Delete Components found
- Recycle Entity ID

However, `SystemStateComponents` are not removed on `DestroyEntity` and the Entity ID is not recycled until the last component is deleted. This gives the System the opportunity to clean up the internal state in the exact same way as with component removal.

## SystemStateComponent

A `SystemStateComponentData` is analogous to a `ComponentData` and used similarly.

```
struct FooStateComponent : ISystemStateComponentData
{
}
```

Visibility of a `SystemStateComponent` is also controlled in the same way as a `Component` (using `private`, `public`, `internal`) However, it's expected, as a general rule, that a `SystemStateComponent` will be `ReadOnly` outside the system that creates it.

## SystemStateSharedComponent

A `SystemStateSharedComponentData` is analogous to a `SharedComponentData` and used similarly.

```
struct FooStateSharedComponent : ISystemStateSharedComponentData
{
  public int Value;
}
```


