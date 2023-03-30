# DOTS User Guide and Frequently Asked Questions

- [Unity.Physics FAQ](https://docs.unity3d.com/Packages/com.havok.physics@latest/index.html?subfolder=/manual/faq.html)


---

Editor tools for working with DOTS

Why use EntityCommandBuffers?

Debugging

Journaling

Be careful about caching and passing some core API types, e.g. ComponentLookup and ComponentTypeHandle. Definitely don't try to store them in components!

Abstracting game code: utility functions, generic jobs, aspects

- [Are Entities a replacement for GameObjects?]()
- [Which parts of DOTS can be used separately from the others?]()
- [How do I create entities and components in my scenes?]()
- [How do I control the creation order and execution order of my systems?]()
- [How do I avoid undermining the performance of Entities?]()
- [Should components be big or small? Is it better to have many components or fewer components?]()
- [Should systems be big or small? Is it better to have many systems or few systems?]()
- [How should I represent state changes of entities?]()
- [What can I use instead of Coroutines?]()
- [How can I handle events?]()
- [How do I debug Burst code?]()
- [Should I enable/disable Burst compilation in development?]()
- [How do manually trigger code generation?]()
- [How do I make a UI in DOTS?]()
- [How do I animate my entities?]()
- [How do I get user input in systems and jobs?]()

# Gotchas

- Not all objects and data returned by the Entities API's are safe to cache. For example, state.WorldUpdateAllocator should be accessed through the property every frame. For example, the array of chunks returned from a query should not be retained: use them immediately then dispose the chunk array [what is a better example?]. Assume you can't cache things unless told otherwise!

# What is DOTS?

- [What is DOTS?](https://youtu.be/391OOZ-Usvc) (4 minute video)
- [Quick Walkthrough of a DOTS Sample](https://youtu.be/XtqKzKE2YYc) (15 minute video. See the DOTS intros linked above for much more depth and detail.)

The Data-Oriented Technology Stack is a set of packages that facilitate writing high-performance code, following Data-Oriented Design (DoD) principles. The core parts are:

- **The C# Job System**: a solution for fast, safe, and easy multi-threading.
- **The Burst compiler package**: an optimizing C# compiler.
- **The Unity.Collections package**: a set of unmanaged collection types.
- **The Unity.Mathematics package**: a math library with special Burst optimizations.
- **The Unity.Entities package**: an implementation of ECS (Entity, Component, System) architecture.

These packages effectively addresses 4 key performance concerns:

- Collections and Entities help avoid garbage collection and thereby avoids its overhead.
- Entities structures data and code in a way that avoids many of the expensive cache misses typical of GameObjects and MonoBehaviours.
- The Job system helps you take advantage of the multiple cores in today's CPU's.
- Burst and Mathematics generate much more efficient machine code than Mono or even IL2CPP.

(Note that DOTS does *not* directly utilize the GPU or address GPU-related performance issues.)

Built on top of the core parts are additional DOTS packages:

- **The Unity.Physics package**: a physics engine for entities.
- **The Unity.Netcode package**: a client-server netcode solution for entities.
- **The Unity.Entities.Graphics package**: renders entities using the Scriptable Rendering Pipeline (URP or HDRP).

Future DOTS packages are planned for animation and audio.

Be clear that a project can utilize both GameObjects and Entities side-by-side:

- A game primarily built with GameObjects might want to use Entities for a special simulation purpose, like say a traffic sim.
- A game primarily built with Entities might still use GameObjects for the sake of UI, animation, audio. (Though again, future packages should make this unnecessary animation and audio.)

Also understand that a game that uses GameObjects without any Entities might still use Jobs, Burst, Collections, and Mathematics for CPU-intensive computation tasks. Just keep in mind that GameObjects and MonoBehaviours are managed objects, so they cannot be directly accessed in jobs and Burst-compiled code, and consequently, you may end up paying some overhead to copy data between managed and unmanaged objects.


# Entities and Components

- when is one component too big or too small
- queries
- modeling state changes
- singletons
- Types of components
- not everything should be components!...except all of your non-entity data should at least be *referenced* from a component

# Baking

# Systems

- `SystemBase` vs `ISystem`
- `Entities.ForEach` or `SystemAPI.Query`
- How much work should be put in a single system? How should work be divided?
- Job dependencies between systems
- storing data per system
- disabling systems

# SystemAPI


# Coordinating between Entities and GameObjects / MonoBehaviours.

# Sound in Entities

# Animation in Entities

# User input in Entities 



## Are entities a replacement for GameObjects?

pros of entities over arrays

- entities can be referenced even if relocated
- resizing is simple
- the parallel arrays of component values are kept in sync for you
- can be queried by component type (what would raw-array equivalent be?)
- standard data protocol (in theory)

pros of arrays over entities

- order within array can be given significance
- easier to defragment? (or avoid fragmentation in the first place)


## Which parts of DOTS can be used separately from the others?



## How do I create entities and components in my scenes?

There is no facility for directly putting entities in a scene. Instead, the GameObjects in special "entity subscenes" are "baked" (converted) at build time into serialized entity data. When a scene loads, its non-baked GameObjects are loaded as usual, and the serialized entities of its entity subscenes are loaded as well.

Even a project that uses only entities at runtime will still generally use "authoring" GameObjects in the editor that get baked into entities.

## How do I control the creation and execution order of my systems?

asdf

## How do I avoid undermining the performance of Entities?

In loose order of significance, these things will undermine the performance benefits of using Entities:

1. Not Burst compiling your code
1. Too many or too frequent structural changes
1. Too many random entity lookups
1. Archetype and chunk fragmentation
1. Too many systems

## Should components be big or small? Is it better to have many components or fewer components?

asdf

## Should systems be big or small? Is it better to have many systems or few systems?

asdf

## How should I represent state changes of entities?

asdf

## What can I use instead of Coroutines?

asdf

## How do I make a UI in DOTS?

asdf

## How can I handle events?

asdf

## How do I debug Burst code?

asdf

## Should I enable/disable Burst compilation in development?


asdf

## How do manually trigger code generation?

## How do I animate my entities?

sadf

## How do I get user input in systems and jobs?

In a system, you can get user input the same as you would in any MonoBehavior or other Unity code. The only limitation is that a system update cannot be Burst compiled if it accesses any managed API's.

As always, a job should only access data that is explicitly passed to it at schedule time. So for a job to read input, the code that schedules the job should pass in the needed input data.

## Should I use managed systems (`SystemBase`) or unmanaged systems (`ISystem`)?

Though not officially deprecated, `SystemBase` should generally only be used in legacy code.

## Should I use `Entities.ForEach` or `SystemAPI.Query`?

Though not officially deprecated, `Entities.ForEach` is only useable in `SystemBase`, not `ISystem`. and should generally only be used in legacy code.




