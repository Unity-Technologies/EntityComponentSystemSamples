# ECS features in detail

## Entity

__Entity__ is an ID. You can think of it as a super lightweight __GameObject__ that does not even have a name by default.

You can add and remove components from Entities at runtime. Entity ID's are stable. In fact they are the only stable way to store a reference to another component or Entity.

## IComponentData

Traditional Unity components (including __MonoBehaviour__) are [object-oriented](https://en.wikipedia.org/wiki/Object-oriented_programming) classes which contain data and methods for behavior. __IComponentData__ is a pure ECS-style component, meaning that it defines no behavior, only data. IComponentDatas are structs rather than classes, meaning that they are copied [by value instead of by reference](https://stackoverflow.com/questions/373419/whats-the-difference-between-passing-by-reference-vs-passing-by-value?answertab=votes#tab-top) by default. You will usually need to use the following pattern to modify data:

```C#
var transform = group.transform[index]; // Read

transform.heading = playerInput.move; // Modify
transform.position += deltaTime * playerInput.move * settings.playerMoveSpeed;

group.transform[index] = transform; // Write
```

IComponentData structs may not contain references to managed objects. Since the all component data lives in simple non-garbage-collected tracked [chunk memory](https://en.wikipedia.org/wiki/Chunking_(computing)).

## EntityArchetype

An __EntityArchetype__ is a unique array of __ComponentType__. __EntityManager__ uses EntityArchetypes to group all Entities using the same ComponentTypes in chunks.

```C#
// Using typeof to create an EntityArchetype from a set of components
EntityArchetype archetype = EntityManager.CreateArchetype(typeof(MyComponentData), typeof(MySharedComponent));

// Same API but slightly more efficient
EntityArchetype archetype = EntityManager.CreateArchetype(ComponentType.Create<MyComponentData>(), ComponentType.Create<MySharedComponent>());

// Create an Entity from an EntityArchetype
var entity = EntityManager.CreateEntity(archetype);

// Implicitly create an EntityArchetype for convenience
var entity = EntityManager.CreateEntity(typeof(MyComponentData), typeof(MySharedComponent));

```

## EntityManager

The EntityManager owns __EntityData__, EntityArchetypes, __SharedComponentData__ and __ComponentGroup__.

EntityManager is where you find APIs to create Entities, check if an Entity is still alive, instantiate Entities and add or remove components.

```cs
// Create an Entity with no components on it
var entity = EntityManager.CreateEntity();

// Adding a component at runtime
EntityManager.AddComponent(entity, new MyComponentData());

// Get the ComponentData
MyComponentData myData = EntityManager.GetComponentData<MyComponentData>(entity);

// Set the ComponentData
EntityManager.SetComponentData(entity, myData);

// Removing a component at runtime
EntityManager.RemoveComponent<MyComponentData>(entity);

// Does the Entity exist and does it have the component?
bool has = EntityManager.HasComponent<MyComponentData>(entity);

// Is the Entity still alive?
bool has = EntityManager.Exists(entity);

// Instantiate the Entity
var instance = EntityManager.Instantiate(entity);

// Destroy the created instance
EntityManager.DestroyEntity(instance);
```

```cs
// EntityManager also provides batch APIs
// to create and destroy many Entities in one call. 
// They are significantly faster 
// and should be used where ever possible
// for performance reasons.

// Instantiate 500 Entities and write the resulting Entity IDs to the instances array
var instances = new NativeArray<Entity>(500, Allocator.Temp);
EntityManager.Instantiate(entity, instances);

// Destroy all 500 entities
EntityManager.DestroyEntity(instances);
```

## Chunk implementation detail

The ComponentData for each Entity is stored in what we internally refer to as a chunk. ComponentData is laid out by stream. Meaning all components of type A, are tightly packed in an array. Followed by all components of type B etc.

A chunk is always linked to a specific EntityArchetype. Thus all Entities in one chunk follow the exact same memory layout. When iterating over components, memory access of components within a chunk is always completely linear, with no waste loaded into cache lines. This is a hard guarantee.

__ComponentDataArray__ is essentially a convenience index based iterator for a single component type;
First we iterate over all EntityArchetypes compatible with the ComponentGroup; for each EntityArchetype iterating over all chunks compatible with it and for each chunk iterating over all Entities in that chunk.

Once all Entities of a chunk have been visited, we find the next matching chunk and iterate through those Entities.

When Entities are destroyed, we move up other Entities into its place and then update the Entity table accordingly. This is required to make a hard guarantee on linear iteration of Entities. The code moving the component data into memory is highly optimized.

## World

A __World__ owns both an EntityManager and a set of __ComponentSystems__. You can create as many Worlds as you like. Commonly you would create a simulation World and rendering or presentation World.

By default we create a single World when entering Play Mode and populate it with all available ComponentSystems in the project, but you can disable the default World creation and replace it with your own code via a global define.

* [Default World creation code](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities.Hybrid/Injection/DefaultWorldInitialization.cs)
* [Automatic bootstrap entry point](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities.Hybrid/Injection/AutomaticWorldBootstrap.cs) 

> Note: We are currently working on multiplayer demos, that will show how to work in a setup with separate simulation & presentation Worlds. This is a work in progress, so right now have no clear guidelines and are likely missing features in ECS to enable it. 

## System update order

In ECS all systems are updated on the main thread. The order in which the are updated is based on a set of constraints and an optimization pass which tries to order the system in a way so that the time between scheduling a job and waiting for it is as long as possible.
The attributes to specify update order of systems are ```[UpdateBefore(typeof(OtherSystem))]``` and ```[UpdateAfter(typeof(OtherSystem))]```. In addition to update before or after other ECS systems it is possible to update before or after different phases of the Unity PlayerLoop by using typeof([UnityEngine.Experimental.PlayerLoop.FixedUpdate](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Experimental.PlayerLoop.FixedUpdate.html)) or one of the other phases in the same namespace.

The UpdateInGroup attribute will put the system in a group and the same __UpdateBefore__ and __UpdateAfter__ attributes can be specified on a group or with a group as the target of the before/after dependency.

To use __UpdateInGroup__ you need to create and empty class and pass the type of that to the __UpdateInGroup__ attribute
```cs
public class UpdateGroup
{}

[UpdateInGroup(typeof(UpdateGroup))]
class MySystem : ComponentSystem
```

## Automatic job dependency management (JobComponentSystem)

Managing dependencies is hard. This is why in __JobComponentSystem__ we are doing it automatically for you. The rules are simple: jobs from different systems can read from IComponentData of the same type in parallel. If one of the jobs is writing to the data then they can't run in parallel and will be scheduled with a dependency between the jobs.

```cs
public class RotationSpeedSystem : JobComponentSystem
{
    [BurstCompile]
    struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed>
    {
        public float dt;

        public void Execute(ref Rotation rotation, [ReadOnly]ref RotationSpeed speed)
        {
            rotation.value = math.mul(math.normalize(rotation.value), quaternion.axisAngle(math.up(), speed.speed * dt));
        }
    }

    // Any previously scheduled jobs reading/writing from Rotation or writing to RotationSpeed 
    // will automatically be included in the inputDeps dependency.
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new RotationSpeedRotation() { dt = Time.deltaTime };
        return job.Schedule(this, 64, inputDeps);
    } 
}
```

### How does this work?

All jobs and thus systems declare what ComponentTypes they read or write to. As a result when a JobComponentSystem returns a __JobHandle__ it is automatically registered with the EntityManager and all the types including the information about if it is reading or writing.

Thus if a system writes to component A, and another system later on reads from component A, then the JobComponentSystem looks through the list of types it is reading from and thus passes you a dependency against the job from the first system.

So JobComponentSystem simply chains jobs as dependencies where needed and thus causes no stalls on the main thread. But what happens if a non-job ComponentSystem accesses the same data? Because all access is declared, the ComponentSystem automatically __Completes__ all jobs running against ComponentTypes that the system uses before invoking __OnUpdate__.

#### Dependency management is conservative & deterministic

Dependency management is conservative. ComponentSystem simply tracks all ComponentGroups ever used and stores which types are being written or read based on that. (So if you inject ComponentDataArrays or use __IJobProcessComponentData__ once but skip using it sometimes, then we will create dependencies against all ComponentGroups that have ever been used by that ComponentSystem.)

Also when scheduling multiple jobs in a single system, dependencies must be passed to all jobs even though different jobs may need less dependencies. If that proves to be a performance issue the best solution is to split a system into two.

The dependency management approach is conservative. It allows for deterministic and correct behaviour while providing a very simple API.

### Sync points

All structural changes have hard sync points. __CreateEntity__, __Instantiate__, __Destroy__, __AddComponent__, __RemoveComponent__, __SetSharedComponentData__ all have a hard sync point. Meaning all jobs scheduled through JobComponentSystem will be completed before creating the Entity, for example. This happens automatically. So for instance: calling __EntityManager.CreateEntity__ in the middle of the frame might result in a large stall waiting for all previously scheduled jobs in the World to complete.

See [EntityCommandBuffer](#entitycommandbuffer) for more on avoiding sync points when creating Entities during game play.

### Multiple Worlds

Every World has its own EntityManager and thus a separate set of Job handle dependency management. A hard sync point in one world will not affect the other world. As a result, for streaming and procedural generation scenarios, it is useful to create entities in one World and then move them to another world in one transaction at the beginning of the frame. 

See [ExclusiveEntityTransaction](#exclusiveentitytransaction) for more on avoiding sync points for procedural generation & streaming scenarios.


## Shared ComponentData

IComponentData is appropriate for data that varies between Entities, such as storing a world position. __ISharedComponentData__ is useful when many Entities have something in common, for example in the boid demo we instantiate many Entities from the same Prefab and thus the __MeshInstanceRenderer__ between many boid Entities is exactly the same. 

```cs
[System.Serializable]
public struct MeshInstanceRenderer : ISharedComponentData
{
    public Mesh                 mesh;
    public Material             material;

    public ShadowCastingMode    castShadows;
    public bool                 receiveShadows;
}
```

In the boid demo we never change the MeshInstanceRenderer component, but we do move all the Entities __TransformMatrix__ every frame.

The great thing about ISharedComponentData is that there is literally zero memory cost on a per Entity basis.

We use ISharedComponentData to group all entities using the same InstanceRenderer data together and then efficiently extract all matrices for rendering. The resulting code is simple & efficient because the data is laid out exactly as it is accessed.

* [MeshInstanceRendererSystem.cs](../../ECSJobDemos/Packages/com.unity.entities/Unity.Rendering.Hybrid/MeshInstanceRendererSystem.cs)

### Some important notes about SharedComponentData:

* Entities with the same SharedComponentData are grouped together in the same chunks. The index to the SharedComponentData is stored once per chunk, not per Entity. As a result SharedComponentData have zero memory overhead on a per Entity basis. 
* Using ComponentGroup we can iterate over all Entities with the same type.
* Additionally we can use __ComponentGroup.SetFilter()__ to iterate specifically over Entities that have a specific SharedComponentData value. Due to the data layout this iteration has low overhead.
* Using __EntityManager.GetAllUniqueSharedComponents__ we can retrieve all unique SharedComponentData that is added to any alive Entities.
* SharedComponentData are automatically [reference counted](https://en.wikipedia.org/wiki/Reference_counting).
* SharedComponentData should change rarely. Changing a SharedComponentData involves using [memcpy](https://msdn.microsoft.com/en-us/library/aa246468(v=vs.60).aspx) to copy all ComponentData for that Entity into a different chunk.

## ComponentSystem

## JobComponentSystem

## Iterating entities

Iterating over all Entities that have a matching set of components, is at the center of the ECS architecture.


## Injection

Injection allows your system to declare its dependencies, while those dependencies are then automatically injected into the injected variables before OnCreateManager, OnDestroyManager and OnUpdate.


#### Component Group Injection

Component Group injection automatically creates a ComponentGroup based on the required Component types.

This lets you iterate over all the entities matching those required Component types.
Each index refers to the same Entity on all arrays.

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

#### ComponentDataFromEntity injection

ComponentDataFromEntity<> can also be injected, this lets you get / set the component data by entity from a job. 


```cs
class PositionSystem : JobComponentSystem
{
    [Inject] ComponentDataFromEntity<Position> m_Positions;
}
```

#### Injecting other systems

```cs
class PositionSystem : JobComponentSystem
{
    [Inject] OtherSystem m_SomeOtherSystem;
}
```

Lastly you can also inject a reference to another system. This will populate the reference to the other system for you.

## ComponentGroup

The ComponentGroup is foundation class on top of which all iteration methods are built (Injection, foreach, IJobProcessComponentData etc)

Essentially a ComponentGroup is constructed with a set of required components, subtractive components. 

The ComponentGroup lets you extract individual arrays. All these arrays are guaranteed to be in sync (same length and the index of each array refers to the same Entity).

Generally speaking GetComponentGroup is used rarely, since ComponentGroup Injection and IJobProcessComponetnData is simpler and more expressive.

However the ComponentGroup API can be used for more advanced use cases like filtering a Component Group based on specific SharedComponent values.

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

## ComponentDataFromEntity

The Entity struct identifies an Entity. If you need to access component data on another Entity, the only stable way of referencing that component data is via the Entity ID. EntityManager provides a simple get & set component data API for it.
```cs
Entity myEntity = ...;
var position = EntityManager.GetComponentData<LocalPosition>(entity);
...
EntityManager.SetComponentData(entity, position);
```

However EntityManager can't be used on a C# job. __ComponentDataFromEntity__ gives you a simple API that can also be safely used in a job.

```cs
// ComponentDataFromEntity can be automatically injected
[Inject]
ComponentDataFromEntity<LocalPosition> m_LocalPositions;

Entity myEntity = ...;
var position = m_LocalPositions[myEntity];
```

## ExclusiveEntityTransaction

EntityTransaction is an API to create & destroy entities from a job. The purpose is to enable procedural generation scenarios where instantiation on big scale must happen on jobs. As the name implies it is exclusive to any other access to the EntityManager.

ExclusiveEntityTransaction should be used on manually created world that acts as a staging area to construct & setup entities.
After the job has completed you can end the EntityTransaction and use ```EntityManager.MoveEntitiesFrom(EntityManager srcEntities);``` to move the entities to an active world.


## EntityCommandBuffer

The command buffer class solves two important problems:

1. When you're in a job, you can't access the entity manager
2. When you access the entity manager (to say, create an entity) you invalidate all injected arrays and component groups

The command buffer abstraction allows you to queue up changes to be performed (from either a job or from the main thread) so that they can take effect later on the main thread. There are two ways to use a command buffer:

1. `ComponentSystem` subclasses which update on the main thread have one available automatically called `PostUpdateCommands`. To use it, simply reference the attribute and queue up your changes. They will be automatically applied to the world immediately after you return from your system's `Update` function.

Here's an example from the two stick shooter sample:

```cs
PostUpdateCommands.CreateEntity(TwoStickBootstrap.BasicEnemyArchetype);
PostUpdateCommands.SetComponent(new Position2D { Value = spawnPosition });
PostUpdateCommands.SetComponent(new Heading2D { Value = new float2(0.0f, -1.0f) });
PostUpdateCommands.SetComponent(default(Enemy));
PostUpdateCommands.SetComponent(new Health { Value = TwoStickBootstrap.Settings.enemyInitialHealth });
PostUpdateCommands.SetComponent(new EnemyShootState { Cooldown = 0.5f });
PostUpdateCommands.SetComponent(new MoveSpeed { speed = TwoStickBootstrap.Settings.enemySpeed });
PostUpdateCommands.AddSharedComponent(TwoStickBootstrap.EnemyLook);
```

As you can see, the API is very similar to the entity manager API. In this mode, it is helpful to think of the automatic command buffer as a convenience that allows you to prevent array invalidation inside your system while still making changes to the world.

2. For jobs, you must request command buffers from a `Barrier` on the main thread, and pass them to jobs. When the barrier system updates, the command buffers will play back on the main thread in the order they were created. This extra step is required so that memory management can be centralized and determinism of the generated entities and components can be guaranteed.

Again let's look at the two stick shooter sample to see how this works in practice.

### Barrier

First, a barrier system is declared:

```cs
public class ShotSpawnBarrier : BarrierSystem
{}
```

There's no code in a barrier system, it just serves as a synchronization point.

Next, we inject this barrier into the system that will request command buffers from it:

```cs
[Inject] private ShotSpawnBarrier m_ShotSpawnBarrier;
```

Now we can access the barrier when we're scheduling jobs and ask for command
buffers from it via `CreateCommandBuffer()`:

```cs
return new SpawnEnemyShots
{
    // ...
    CommandBuffer = m_ShotSpawnBarrier.CreateCommandBuffer(),
    // ...
}.Schedule(inputDeps);
```

In the job, we can use the command buffer normally:

```cs
CommandBuffer.CreateEntity(ShotArchetype);
CommandBuffer.SetComponent(spawn);
```

When the barrier system updates, it will automatically play back the command buffers. It's worth noting that the barrier system will take a dependency on any jobs spawned by systems that access it (so that it can now that the command buffers have been filled in fully). If you see bubbles in the frame, it may make sense to try moving the barrier later in the frame, if your game logic allows for this.

### Using command buffers from parallel for-style jobs

To record entity command buffers from parallel for jobs, you must use the `EntityCommandBuffer.Concurrent` type.

## GameObjectEntity

ECS ships with the __GameObjectEntity__ component. It is a MonoBehaviour. In __OnEnable__, the GameObjectEntity component creates an Entity with all components on the GameObject. As a result the full GameObject and all its components are now iterable by ComponentSystems.

TODO: what do you mean by "the GameObjectEntity component creates an Entity with all components on the GameObject" in the sentence above? do you mean all required components that this GameObject should have in that particular case? or is there a pre-defined set of components that will always be added? It's a little unclear.

> Note: for the time being, you must add a GameObjectEntity component on each GameObject that you want to be visible / iterable from the ComponentSystem.

## SystemStateComponentData

The purpose of SystemStateComponentData is to allow you to track resources internal to a system and have the opportunity to appropriately create and destroy those resources as needed without relying on individual callbacks.

SystemStateComponentData and SystemStateSharedComponentData are exactly like ComponentData and SharedComponentData, respectively, except in one important respect:
1. SystemState components are not deleted when an entity is destroyed.

DestroyEntity is shorthand for
1. Find all Components which reference this particular Entity ID
2. Delete those Components
3. Recycle the Entity id for reuse.

However, if SystemState components are present, they are not removed. This gives a system the opportunity to cleanup any resources or state associated with an Entity ID. The Entity id will only be reused once all SystemState components have been removed.

