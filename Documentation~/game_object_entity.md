# GameObjectEntity

ECS ships with the `GameObjectEntity` component. It is a [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html). In `OnEnable`, the `GameObjectEntity` component creates an `Entity` with all components on the `GameObject`. As a result the full `GameObject` and all its components are now iterable by `ComponentSystem` classes.

> Note: for the time being, you must add a `GameObjectEntity` component on each `GameObject` that you want to be visible / iterable from the `ComponentSystem`.

[Back to Unity Data-Oriented reference](reference.md)