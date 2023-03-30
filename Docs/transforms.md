# Transform components and systems

The [`LocalTransform`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Transforms.LocalTransform.html) component represents the transform of an entity, and entity transform hierarchies are formed with three additional components:

- The [`Parent`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Transforms.Parent.html) component stores the id of the entity's parent.
- The [`Child`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Transforms.Child.html) dynamic buffer component stores the ids of the entity's children.
- The [`PreviousParent`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Transforms.PreviousParent.html) component stores a copy of the id of the entity's parent.

To modify the transform hierarchy:

- Add the `Parent` component to parent an entity.
- Remove an entity's `Parent` component to de-parent it.
- Set an entity's `Parent` component to change its parent.

The [`ParentSystem`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Transforms.ParentSystem.html) updates the `Child` and `PreviousParent` components to ensure that:

- Every entity with a parent has a `PreviousParent` component that references the parent.
- Every entity with one or more children has a `Child` buffer component that references all of its children.

| &#x26A0; IMPORTANT |
| :- |
| Although you can safely *read* an entity's `Child` buffer component, you should not modify it directly. Only modify the transform hierarchy by setting the entities' `Parent` components. |

Every frame, the [`LocalToWorldSystem`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Transforms.LocalToWorldSystem.html) computes each entity's world-space transform (from the `LocalTransform` components of the entity and its ancestors) and assigns it to the entity's [`LocalToWorld`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Transforms.LocalToWorld.html) component.

| &#x1F4DD; NOTE |
| :- |
| The `Entity.Graphics` systems read the `LocalToWorld` component but not any of the other transform components, so `LocalToWorld` is the only transform component an entity needs to be rendered. |