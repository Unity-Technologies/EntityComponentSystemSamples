# HelloCube: HybridComponent

***Hybrid Components are an experimental feature, their use isn't recommended yet.***

In this sample, we create instances of an ECS prefab converted from GameObjects whose MonoBehaviour Components have no ECS equivalent. During conversion, those Components are "carried over" and still exist as MonoBehaviours attached to the entities as "Hybrid Components".

## What does it show?

The "CubePrefab" contains two cubes that both have a `WireframeGizmo` component. This component displays a wireframe sphere, making it easy to check that the component is there and that it follows its GameObject.

By default, UnityEngine Components are dropped during conversion. In order to allow `WireframeGizmo` to be kept as an hybrid component, `WireframeGizmoConversionSystem` does an explicit call to `AddHybridComponent`.

```C#
public class WireframeGizmoConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((WireframeGizmo wireframeGizmo) =>
        {
            AddHybridComponent(wireframeGizmo);
        });
    }
}
```

Each resulting entity has a "Companion GameObject" which contains only the Hybrid Components for that entity. That GameObject should be considered an implementation detail and never accessed directly. It will be transformed, created and destroyed automatically.