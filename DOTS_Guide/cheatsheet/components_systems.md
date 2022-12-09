<!---
This file has been generated from components_systems.src.md
Do not modify manually! Use the following tool:
https://github.com/Unity-Technologies/dots-tutorial-processor
-->
# **IComponentData with authoring component and a Baker**

```c#
// An example component for which we want to
// define an authoring component and a baker.
public struct EnergyShield : IComponentData
{
    public int HitPoints;
    public int MaxHitPoints;
    public float RechargeDelay;
    public float RechargeRate;
}

// An authoring component for EnergyShield.
// By itself, an authoring component is just an ordinary MonoBehavior.
public class EnergyShieldAuthoring : MonoBehaviour
{
    // Notice the authoring component has no HitPoints field.
    // This is fine as long as we don't need to set the HitPoints
    // value in the editor.

    // The fact that these names mirror the fields
    // of EnergyShield is not a requirement.

    public int MaxHitPoints;
    public float RechargeDelay;
    public float RechargeRate;
}

// The baker for our EnergyShield authoring component.
// For every GameObject in an entity subscene, baking creates a
// corresponding entity. This baker is run once for every
// EnergyShieldAuthoring instance that's attached to any GameObject in
// the entity subscene.
public class EnergyShieldBaker : Baker<EnergyShieldAuthoring>
{
    public override void Bake(EnergyShieldAuthoring authoring)
    {
        // This simple baker adds just one component to the entity.
        AddComponent(new EnergyShield {
            HitPoints = authoring.MaxHitPoints,
            MaxHitPoints = authoring.MaxHitPoints,
            RechargeDelay =  authoring.RechargeDelay,
            RechargeRate = authoring.RechargeRate,
        });
    }
}
```

<br/>

# **System and SystemGroup**

```c#
// An example system.
// This system will be added to the system group called MySystemGroup.
// The ISystem methods are made Burst 'entry points' by marking
// them and the struct itself with the BurstCompile attribute.
[BurstCompile]
[UpdateInGroup(typeof(MySystemGroup))]
public partial struct MySystem : ISystem
{
    // Called once when the system is created.
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    // Called once when the system is destroyed.
    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    // Usually called every frame. When exactly a system is updated
    // is determined by the system group to which it belongs.
    [BurstCompile]
    public void OnUpdate(ref SystemState state) { }
}

// An example system group.
public class MySystemGroup : ComponentSystemGroup
{
    // A system group is left empty unless you want
    // to override OnUpdate, OnCreate, or OnDestroy.
}
```

<br/>

# **Creating and destroying entities; adding and removing components**

*If you wish to get and set components of many entities, it is best to iterate through all the entities matched by a query. See below.*

```c#
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // The EntityManager of the World to which this system belongs.
        EntityManager em = state.EntityManager;

        // Create a new entity with no components.
        Entity entity = em.CreateEntity();

        // Add components to the entity.
        em.AddComponent<Foo>(entity);
        em.AddComponent<Bar>(entity);

        // Remove a component from the entity.
        em.RemoveComponent<Bar>(entity);

        // Set the entity's Foo value.
        em.SetComponentData<Foo>(entity, new Foo { });

        // Get the entity's Foo value.
        Foo foo = em.GetComponentData<Foo>(entity);

        // Check if the entity has a Bar component.
        bool hasBar = em.HasComponent<Bar>(entity);

        // Destroy the entity.
        em.DestroyEntity(entity);

        // Define an archetype.
        var types = new NativeArray<ComponentType>(3, Allocator.Temp);
        types[0] = ComponentType.ReadWrite<Foo>();
        types[1] = ComponentType.ReadWrite<Bar>();
        EntityArchetype archetype = em.CreateArchetype(types);

        // Create a second entity with the components Foo and Bar.
        Entity entity2 = em.CreateEntity(archetype);

        // Create a third entity by copying the second.
        Entity entity3 = em.Instantiate(entity2);
    }
}
```

<br/>

# **Querying for entities**

```c#
[BurstCompile]
public partial struct MySystem : ISystem
{
    // We need type handles to access a chunk's
    // component arrays and entity ID array.
    // It's generally good practice to cache queries and type handles
    // rather than re-retrieving them every update.
    private EntityQuery myQuery;
    private ComponentTypeHandle<Foo> fooHandle;
    private ComponentTypeHandle<Bar> barHandle;
    private EntityTypeHandle entityHandle;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<Foo, Bar, Apple>();
        builder.WithNone<Banana>();
        myQuery = state.GetEntityQuery(builder);

        ComponentTypeHandle<Foo> fooHandle = state.GetComponentTypeHandle<Foo>();
        ComponentTypeHandle<Bar> barHandle = state.GetComponentTypeHandle<Bar>();
        EntityTypeHandle entityHandle = state.GetEntityTypeHandle();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Type handles must be updated before use in each update.
        fooHandle.Update(ref state);
        barHandle.Update(ref state);
        entityHandle.Update(ref state);

        // Getting array copies of component values and entity ID's:
        {
            // Remember that Temp allocations do not need to be manually disposed.

            // Get an array of the Apple component values of all
            // entities that match the query.
            // This array is a *copy* of the data stored in the chunks.
            NativeArray<Apple> apples = myQuery.ToComponentDataArray<Apple>(Allocator.Temp);

            // Get an array of the ID's of all entities that match the query.
            // This array is a *copy* of the data stored in the chunks.
            NativeArray<Entity> entities = myQuery.ToEntityArray(Allocator.Temp);
        }

        // Getting the chunks matching a query and accessing the chunk data:
        {
            // Get an array of all chunks matching the query.
            NativeArray<ArchetypeChunk> chunks = myQuery.ToArchetypeChunkArray(Allocator.Temp);

            // Loop over all chunks matching the query.
            for (int i = 0, chunkCount = chunks.Length; i < chunkCount; i++)
            {
                ArchetypeChunk chunk = chunks[i];

                // The arrays returned by `GetNativeArray` are the very same arrays
                // stored within the chunk, so you should not attempt to dispose of them.
                NativeArray<Foo> foos = chunk.GetNativeArray(fooHandle);
                NativeArray<Bar> bars = chunk.GetNativeArray(barHandle);

                // Unlike component values, entity ID's should never be
                // modified, so the array of entity ID's is always read only.
                NativeArray<Entity> entities = chunk.GetNativeArray(entityHandle);

                // Loop over all entities in the chunk.
                for (int j = 0, entityCount = chunk.Count; j < entityCount; j++)
                {
                    // Get the entity ID and Foo component of the individual entity.
                    Entity entity = entities[j];
                    Foo foo = foos[j];
                    Bar bar = bars[j];

                    // Set the Foo value.
                    foos[j] = new Foo { };
                }
            }
        }

        // SystemAPI.Query provides a more convenient way to loop
        // through the entities matching a query. Source generation
        // translates this foreach into the functional equivalent
        // of the prior section. Understand that SystemAPI.Query
        // should ONLY be called as the 'in' clause of a foreach.
        {
            // Each iteration processes one entity matching a query
            // that includes Foo, Bar, Apple and excludes Banana:
            // - 'foo' is assigned a read-write reference to the Foo component
            // - 'bar' is assigned a read-only reference to the Bar component
            // - 'entity' is assigned the entity ID
            foreach (var (foo, bar, entity) in
                     SystemAPI.Query<RefRW<Foo>, RefRO<Bar>>()
                         .WithAll<Apple>().WithNone<Banana>().WithEntityAccess())
            {
                foo.ValueRW = new Foo { };
            }
        }
    }
}
```

<br/>

# **EntityCommandBufferSystems**

```c#
// Define a new EntityCommandBufferSystem that will update
// in the InitializationSystemGroup before FooSystem.
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(FooSystem))]
public class MyEntityCommandBufferSystem : EntityCommandBufferSystem { }
```

There's not usually any reason to override the methods of `EntityCommandBufferSystem`, so an empty class, like the one above, is the norm. In fact, needing to create your own `EntityCommandBufferSystem` is uncommon in the first place because the default world already includes several, such as `BeginSimulationEntityCommandBufferSystem`.

<br/>

# **DynamicBuffers (IBufferElementData)**

```c#
// Defines a DynamicBuffer<Waypoint> component type,
// which is a growable array of Waypoint elements.
// InternalBufferCapacity is the number of elements per
// entity stored directly in the chunk (defaults to 8).
[InternalBufferCapacity(20)]
public struct Waypoint : IBufferElementData
{
    public float3 Value;
}

// Example of creating and accessing a DynamicBuffer in a system.
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity entity = state.EntityManager.CreateEntity();

        // Adds the Waypoint component type to the entity
        // and returns the new buffer.
        DynamicBuffer<Waypoint> waypoints = state.EntityManager.AddBuffer<Waypoint>(entity);

        // Setting the length to a value greater than its current capacity resizes the buffer.
        waypoints.Length = 100;

        // Loop through the buffer to set its values.
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i] = new Waypoint { Value = new float3() };
        }
    }
}
```

<br/>

# **Enableable components**

```c#
// An example enableable component type.
// Structs implementing IComponentData or IBufferElementData
// can also implement IEnableableComponent.
public struct Health : IComponentData, IEnableableComponent
{
    public float Value;
}
```

`EntityManager`, `ComponentLookup<T>`, and `ArchetypeChunk` all have methods for checking and setting the enabled state of components:

```c#
// A system demonstrating use of an enableable component type.
[BurstCompile]
public partial struct MySystem : ISystem
{
    private EntityQuery myQuery;
    private ComponentLookup<Health> healthLookup;
    private ComponentTypeHandle<Health> healthHandle;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<Health>();
        myQuery = state.GetEntityQuery(builder);

        healthLookup = state.GetComponentLookup<Health>();
        healthHandle = state.GetComponentTypeHandle<Health>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;

        // Component lookups and type handles should be
        // updated before use every update.
        healthLookup.Update(ref state);
        healthHandle.Update(ref state);

        // Create a single entity and add the Health component.
        Entity myEntity = em.CreateEntity();
        em.AddComponent<Health>(myEntity);

        // Components begin life enabled,
        // so this returns true.
        bool b = em.IsComponentEnabled<Health>(myEntity);

        // Disable the Health component of myEntity
        em.SetComponentEnabled<Health>(myEntity, false);

        // Though disabled, the component can still be read and modified.
        Health h = healthLookup[myEntity];

        // The returned array will not include myEntity.
        var entities = myQuery.ToEntityArray(Allocator.Temp);

        // The returned array will not include the Health of myEntity.
        var healths = myQuery.ToComponentDataArray<Health>(Allocator.Temp);

        // We can check and set the enabled state
        // through the ComponentLookup.
        b = healthLookup.IsComponentEnabled(myEntity);
        healthLookup.SetComponentEnabled(myEntity, false);

        // Get the chunks matching the query.
        var chunks = myQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];

            // Loop through the entities of the chunk.
            for (int entityIdx = 0, entityCount = chunk.Count; entityIdx < entityCount; entityIdx++)
            {
                // Read the enabled state of the
                // entity's Health component.
                bool enabled = chunk.IsComponentEnabled(healthHandle, entityIdx);

                // Disable the entity's Health component.
                chunk.SetComponentEnabled(healthHandle, entityIdx, false);
            }
        }
    }
}
```

<br/>

# **Aspects**

```c#
// An example aspect which wraps a Foo component,
// the enabled state of a Bar component,
// and the components of the TransformAspect.
public readonly partial struct MyAspect : IAspect
{
    // The aspect includes the entity ID.
    // Because it's a readonly value type,
    // there's no danger in making the field public.
    public readonly Entity Entity;

    // The aspect includes the Foo component,
    // with read-write access.
    private readonly RefRW<Foo> foo;

    // A property which gets and sets the Speed.
    public float3 Foo
    {
        get => foo.ValueRO.Value;
        set => foo.ValueRW.Value = value;
    }

    // The aspect includes the enabled state of
    // the Bar component.
    public readonly EnabledRefRW<Bar> BarEnabled;

    // The aspect includes Unity.Entities.TransformAspect.
    // This means MyAspect indirectly includes
    // all the components of TransformAspect.
    private readonly TransformAspect transform;

    // A property which gets and sets the position of the transform.
    // The TransformAspect is otherwise kept private,
    // so users of MyAspect will only be able to
    // read and modify the position.
    public float3 Position
    {
        get => transform.Position;
        set => transform.Position = value;
    }
}
```

These methods return instances of an aspect:

- `SystemAPI.GetAspectRW<T>(Entity)`
- `SystemAPI.GetAspectRO<T>(Entity)`
- `EntityManager.GetAspect<T>(Entity)`
- `EntityManager.GetAspectRO<T>(Entity)`

These methods throw if the `Entity` passed doesn't have all the components included in aspect `T`.

Aspects returned by `GetAspectRO()` will throw if you use any method or property that attempts to modify the underlying components.

You can also get aspect instances by including them as parameters of an `IJobEntity`'s `Execute` method or as type parameters of `SystemAPI.Query`.

<br/>
