# ComponentDataFromEntity

The `Entity` struct identifies an entity. If you need to access `ComponentData` on another `Entity`, the only stable way of referencing that `ComponentData` is via the `Entity` ID. `EntityManager` provides a simple get & set `ComponentData` API for it.

```cs
Entity myEntity = ...;
var position = EntityManager.GetComponentData<LocalPosition>(entity);
...
EntityManager.SetComponentData(entity, position);
```

However, `EntityManager` can't be used in a C# job. `ComponentDataFromEntity` gives you a simple API that can also be safely used in a job.

```cs
// ComponentDataFromEntity can be automatically injected
[Inject]
ComponentDataFromEntity<LocalPosition> m_LocalPositions;

Entity myEntity = ...;
var position = m_LocalPositions[myEntity];
```

[Back to Unity Data-Oriented reference](reference.md)