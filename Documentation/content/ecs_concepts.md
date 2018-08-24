# ECS concepts

If you are familiar with [Entity-component-system](https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system) (ECS) concepts, you might see the potential for naming conflicts with Unity's existing __GameObject__/__Component__ setup. 

The purpose of this page is:
1. Clarify and disambiguate the concepts as used in the ECS.
2. Provide a brief introduction to each concept as an entry point to a new user.

### EntityManager
Manages memory and structural changes.

### ComponentData
Parallel streams of concrete, [blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types) data. 

e.g.

| Position | HitPoints |
| ---------- | -----------|
| 64,30     | 69          |
| 58,55     | 70          |
| 95,81     | 81          |
| 10,50     | 19          |
| 36,24     | 38          |
| 67,33     | 40          |

See: [IComponentData in detail](ecs_in_detail.md#icomponentdata)


### Entity
An ID which can be used for indirect component lookups for the purposes of graph traversal.

e.g.

| Entity | Position | HitPoints |
|--- | ---------- | -----------|
|0 | 64,30     | 69          |
|1 | 58,55     | 70          |
|2 | 95,81     | 81          |
|3 | 10,50     | 19          |
|4 | 36,24     | 38          |
|5 | 67,33     | 40          |

See: [Entity in detail](ecs_in_detail.md#entity)

### SharedComponentData
Type of ComponentData where each unique value is only stored once. ComponentData streams are divided into subsets by each value of all SharedComponentData.

e.g. (Mesh SharedComponentData)

__Mesh = RocketShip__

| Position | HitPoints |
| ---------- | -----------|
| 64,30     | 69          |
| 58,55     | 70          |
| 95,81     | 81          |

__Mesh = Bullet__

| Position | HitPoints |
| ---------- | -----------|
| 10,50     | 19          |
| 36,24     | 38          |
| 67,33     | 40          |

See: [SharedComponentData in detail](ecs_in_detail.md#shared-componentdata)

### Dynamic Buffers

This is a type of component data that allows a variable-sized, "stretchy"
buffer to be associated with an entity. Behaves as a component type that
carries an internal capacity of a certain number of elements, but can allocate
a heap memory block if the internal capacity is exhausted.

See: [Dynamic Buffers](dynamic_buffers.md)

### EntityArchetype
Specific set of ComponentData types and SharedComponentData values which define the subsets of ComponentData streams stored in the EntityManager.

e.g. In the above, there are two EntityArchetypes:
1. Position, HitPoints, Mesh = RocketShip
2. Position, HitPoints, Mesh = Bullet

See: [EntityArchetype in detail](ecs_in_detail.md#entityarchetype)

### ComponentSystem
Where gameplay/system logic/behavior occurs.

See: [ComponentSystem in detail](ecs_in_detail.md#componentsystem)

### World
A unique EntityManager with specific instances of defined ComponentSystems. Multiple Worlds may exist and work on independent data sets.

See: [World in detail](ecs_in_detail.md#world)

### SystemStateComponentData
A specific type of ComponentData which is not serialized or removed by default when an Entity ID is deleted. Used for internal state and resource management inside a system. Allows you to manage construction and destruction of resources.

See: [SystemStateComponentData in detail](ecs_in_detail.md#systemstatecomponentdata)

### JobComponentSystem
A type of ComponentSystem where jobs are queued independently of the JobComponentSystem's update, in the background. Those jobs are guaranteed to be completed in the same order as the systems. 

See: [JobComponentSystem in detail](ecs_in_detail.md#jobcomponentsystem)

### EntityCommandBuffer
A list of structural changes to the data in an EntityManager for later completion. Structural changes are:
1. Adding Component
2. Removing Component
3. Changing SharedComponent value

See: [EntityCommandBuffer in detail](ecs_in_detail.md#entitycommandbuffer)

### Barrier
A type of ComponentSystem, which provides an EntityCommandBuffer. i.e. A specific (synchronization) point in the frame where that EntityCommandBuffer will be resolved.

See: [Barrier in detail](ecs_in_detail.md#barrier)




