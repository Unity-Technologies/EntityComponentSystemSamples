# Deprecation warning

Injection is on its way out, it is not recommended to use to the `[Inject]` attribute anymore.

* Component group injection is replaced by `IJobProcessComponentData`, [chunk iteration](chunk_iteration.md), [ForEach](getting_started.md), and `(Job)ComponentSystem.GetComponentGroup`.
* ComponentDataFromEntity injection is replaced by `(Job)ComponentSystem.GetComponentDataFromEntity`.
* Injecting other systems is replaced by `(Job)ComponentSystem.World.GetOrCreateManager`.

```cs
class MySystem : JobComponentSystem
{
    // Deprecated Injection-based setup. Moved to a ComponentGroup.
    //public struct Group
    //{
    //    // ComponentDataArray lets us access IComponentData 
    //    public ComponentDataArray<Position> Position;

    //    [ReadOnly]
    //    public ComponentDataArray<Velocity> Velocity;

    //    // The Length can be injected for convenience as well 
    //    public int Length;
    //}
    //[Inject] private Group m_Group;

    struct MoveJob : IJobProcessComponentData<Position, Velocity>
    {
        public float dt;

        public void Execute(ref Position position, [ReadOnly] ref Velocity velocity)
        {
            position = new Position { position.value + velocity.value * dt };
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new MoveJob
        {
            dt = Time.deltaTime
        };

        return job.Schedule(this, inputDeps);
    }
}
```

```cs
class PositionSystem : JobComponentSystem
{
    private OtherSystem m_SomeOtherSystem;

    protected override void OnCreateManager()
    {
        m_SomeOtherSystem = World.GetOrCreateManager<OtherSystem>();
    }
}
```

```cs
class PositionSystem : JobComponentSystem
{
    //[Inject] ComponentDataFromEntity<Position> m_Positions;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Moved to a local variable in OnUpdate (must be called every frame)
        var positions = GetComponentDataFromEntity<Position>();

        ...
    }
}
```

# Injection

Injection allows your system to declare its dependencies, while those dependencies are then automatically injected into the injected variables before `OnCreateManager`, `OnDestroyManager`, and `OnUpdate`.

## Component Group Injection

`ComponentGroup` injection automatically creates a `ComponentGroup` based on the required component types.

This lets you iterate over all the entities matching those required component types.
Each index refers to the same `Entity` on all arrays.

```cs
class MySystem : ComponentSystem
{
    public struct Group
    {
        // ComponentDataArray lets us access IComponentData 
        [ReadOnly]
        public ComponentDataArray<Position> Position;
        
        // ComponentArray lets us access any of the existing class Component                
        public ComponentArray<Rigidbody> Rigidbodies;

        // Sometimes it is necessary to not only access the components
        // but also the Entity ID.
        public EntityArray Entities;

        // The GameObject Array lets us retrieve the game object.
        // It also constrains the group to only contain GameObject based entities.                  
        public GameObjectArray GameObjects;

        // Excludes entities that contain a MeshCollider from the group
        public SubtractiveComponent<MeshCollider> MeshColliders;
        
        // The Length can be injected for convenience as well 
        public int Length;
    }
    [Inject] private Group m_Group;


    protected override void OnUpdate()
    {
        // Iterate over all entities matching the declared ComponentGroup required types
        for (int i = 0; i != m_Group.Length; i++)
        {
            m_Group.Rigidbodies[i].position = m_Group.Position[i].Value;

            Entity entity = m_Group.Entities[i];
            GameObject go = m_Group.GameObjects[i];
        }
    }
}
```

## ComponentDataFromEntity injection

`ComponentDataFromEntity<>` can also be injected, this lets you get / set the `ComponentData` by `Entity` from a job. 

```cs
class PositionSystem : JobComponentSystem
{
    [Inject] ComponentDataFromEntity<Position> m_Positions;
}
```

## Injecting other systems

```cs
class PositionSystem : JobComponentSystem
{
    [Inject] OtherSystem m_SomeOtherSystem;
}
```

Lastly you can also inject a reference to another system. This will populate the reference to the other system for you.

[Back to Unity Data-Oriented reference](reference.md)
