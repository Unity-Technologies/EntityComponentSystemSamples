# TransformSystem

`TransformSystem` is responsible for updating the `LocalToWorld` transformation matrices used by other systems (including rendering).

## Updating a TransformSystem

By default, Unity updates a single `TransformSystem` instance each frame. `EndFrameTransformSystem` is updated before `EndFrameBarrier` and requires no additional work.

In cases where transform data is required to be updated at additional points in the frame, the suggested methods are:

- Create additional instance(s) of `TransformSystem` and use `UpdateBefore`/`UpdateAfter` attributes to control where they are updated.

For example:

```
    [UpdateBefore(typeof(EndFrameBarrier))]
    public class EndFrameTransformSystem : TransformSystem<EndFrameBarrier>
    {
    }
```
- Create additional instance of `TransformSystem` and update after **every** `ComponentSystem` (includes barriers, excludes `JobComponentSystem`) by using `[ComponentSystemPatch]` attribute. Be aware that the overhead for updating the `TransformSystem` after every `ComponentSystem` can be very high when the number of `ComponentSystem`classes is large.

For example:

```
    [ComponentSystemPatch]
    public class PatchTransformSystem : TransformSystem<UserSystem>
    {
    }
```

- Manually update your systems. This is the expected method for more complex applications.

## Updating Position, Rotation, Scale

The only requirement for `TransfromSystem` is that **one of** the following components is associated with an `Entity`:

```
    public struct Position : IComponentData
    {
        public float3 Value;
    }

    public struct Rotation : IComponentData
    {
        public quaternion Value;
    }

    public struct Scale : IComponentData
    {
        public float3 Value;
    }
```

`TransformSystem` will add the `LocalToWorld` component and update the matrix based on the values in your selected associated components (__Position__, __Rotation__, and __Scale__). `LocalToWorld` does not need to be added by the user or other systems.

```
    public struct LocalToWorld : ISystemStateComponentData
    {
        public float4x4 Value;
    }
```

`LocalToWorld` is expected to be `[ReadOnly]` by other systems. Be aware that anything written to this component will be overwritten and its behavior during update is undefined if written to.

## Freezing transforms

In many cases, individual transforms are *never* expected to change at runtime. For these types of objects, if a `Static` component is added:

```
    public struct Static : IComponentData
    {
    }
```

They will cease to be updated after the `LocalToWorld` matrix has been created and updated for the first time. Any changes to associated Position, Rotation, or Scale components will be ignored. Marking `Static` transforms can substantially reduce the amount of work a `TransformSystem` needs to do which will improve performance.

The process of freezing is:
1. `TransformSystem` update queries for `Static` components.
2. `TransformSystem` adds `FrozenPending` component.
3. `TransformSystem` updates the `LocalToWorld` component normally.
4. On the next `TransformSystem` update, queries for `FrozenPending` components and adds `Frozen` component.
5. `TransformSystem` ignores Position, Rotation, or Scale components if there is an associated `Frozen` component.

If necessary, users can detect if `LocalToWorld` matrix is frozen by existence of a `Frozen` component. Generally however, identifying static objects by the existence of the `Static` component is sufficient.

## Custom transforms

In cases where user systems require custom transformation matrices and updates, the `TransformSystem` will ignore components associated with a `CustomLocalToWorld` component.

```
    public struct CustomLocalToWorld : IComponentData
    {
        public float4x4 Value;
    }
```

If a `CustomLocalToWorld` component exists, it is expected that a user system will write the appropriate data. Position, Rotation, Scale, `Static`, and any other `TransformSystem` components are ignored.

## Attaching transformations

Attaching transformations (transformation hierarchies) is controlled by separate "event" or "side-channel" entities associated with an `Attach` component.

```
    public struct Attach : IComponentData
    {
        public Entity Parent;
        public Entity Child;
    }
```

To attach a Child `Entity` to a Parent `Entity`, create a new `Entity` with an associated Attach component, assigning the respective values.

For example:

```
    var parent = m_Manager.CreateEntity(typeof(Position), typeof(Rotation));
    var child = m_Manager.CreateEntity(typeof(Position));
    var attach = m_Manager.CreateEntity(typeof(Attach));

    m_Manager.SetComponentData(parent, new Position {Value = new float3(0, 2, 0)});
    m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(1.0f, 0.0f, 0.0f), math.up())});
    m_Manager.SetComponentData(child, new Position {Value = new float3(0, 0, 1)});
    m_Manager.SetComponentData(attach, new Attach {Parent = parent, Child = child});
```

1. The `Attach` component, and associated `Entity`, will be destroyed by the `TransformSystem` on update.
2. Values in Position, Rotation, and Scale components will be interpreted as relative to parent space.`
3. Attached, Parent, and LocalToParent components will be associated with the Child `Entity`.

```
    public struct Attached : IComponentData
    {
    }

    public struct Parent : ISystemStateComponentData
    {
        public Entity Value;
    }

    public struct LocalToParent : ISystemStateComponentData
    {
        public float4x4 Value;
    }
```

## Detaching transformations

To detach a child from a parent, remove the `Attached` component from the child. When the `TransformSystem` is updated, the Parent component will be removed. Values in Position, Rotation, and Scale components will be interpreted as relative to `World` space.

## Reading World values

The `LocalToWorld` matrix can be used to retrieve `World` positions.
For example:

```
var childWorldPosition = m_Manager.GetComponentData<LocalToWorld>(child).Value.c3
```

[Back to Unity Data-Oriented reference](reference.md)