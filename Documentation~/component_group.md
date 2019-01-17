## ComponentGroup

The `ComponentGroup` is a foundation class on top of which all iteration methods are built (Injection, `foreach`, `IJobProcessComponentData` etc)

Essentially a `ComponentGroup` is constructed with a set of required components, and/or subtractive components. 

The `ComponentGroup` lets you extract individual arrays. All these arrays are guaranteed to be in sync (same length and the index of each array refers to the same `Entity`).

Generally speaking `GetComponentGroup` is used rarely, since `ComponentGroup` Injection and `IJobProcessComponetnData` is simpler and more expressive.

However the `ComponentGroup` API can be used for more advanced use cases like filtering a `Component Group` based on specific `SharedComponent` values.

```cs
struct SharedGrouping : ISharedComponentData
{
    public int Group;
}

class PositionToRigidbodySystem : ComponentSystem
{
    ComponentGroup m_Group;

    protected override void OnCreateManager(int capacity)
    {
        // GetComponentGroup should always be cached from OnCreateManager, never from OnUpdate
        // - ComponentGroup allocates GC memory
        // - Relatively expensive to create
        // - Component type dependencies of systems need to be declared during OnCreateManager,
        //   in order to allow automatic ordering of systems
        m_Group = GetComponentGroup(typeof(Position), typeof(Rigidbody), typeof(SharedGrouping));
    }

    protected override void OnUpdate()
    {
        // Only iterate over entities that have the SharedGrouping data set to 1
        // (This could for example be used as a form of gamecode LOD)
        m_Group.SetFilter(new SharedGrouping { Group = 1 });
        
        var positions = m_Group.GetComponentDataArray<Position>();
        var rigidbodies = m_Group.GetComponentArray<Rigidbody>();

        for (int i = 0; i != positions.Length; i++)
            rigidbodies[i].position = positions[i].Value;
            
        // NOTE: GetAllUniqueSharedComponentDatas can be used to find all unique shared components 
        //       that are added to entities. 
        // EntityManager.GetAllUniqueSharedComponentDatas(List<T> shared);
    }
}
```

[Back to Unity Data-Oriented reference](reference.md)