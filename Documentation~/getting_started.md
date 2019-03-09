# Getting started 

## What are we trying to solve?

When making games with __GameObject__/__MonoBehaviour__, it is easy to write code that ends up being difficult to read, maintain and optimize. This is the result of a combination of factors: [object-oriented model](https://en.wikipedia.org/wiki/Object-oriented_programming), non-optimal machine code compiled from Mono, [garbage collection](https://en.wikipedia.org/wiki/Garbage_collection_(computer_science)) and [single threaded](https://en.wikipedia.org/wiki/Thread_(computing)#Single_threading) code.

## Entity-component-system to the rescue

An [Entity-component-system](https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system) (ECS) is a way of writing code that focuses on the actual problems you are solving: the data and behavior that make up your game.

In addition to being a better way of approaching game programming for design reasons, using ECS puts you in an ideal position to leverage Unity's job system and Burst compiler, letting you take full advantage of today's multicore processors.

We have exposed Unity's native job system so that users can gain the benefits of [multithreaded](https://en.wikipedia.org/wiki/Thread_(computing)) batch processing from within their ECS C# scripts. The native job system has built in safety features for detecting [race conditions](https://en.wikipedia.org/wiki/Race_condition).

However we need to introduce a new way of thinking and coding to take full advantage of the job system.

## What is ECS?

### MonoBehavior - A dear old friend

MonoBehaviours contain both the data and the behaviour. This component will simply rotate the __Transform__ component every frame.

```C#
using UnityEngine;

class Rotator : MonoBehaviour
{
    // The data - editable in the inspector
    public float speed;
    
    // The behaviour - Reads the speed value from this component 
    // and changes the rotation of the Transform component.
    void Update()
    {
        transform.rotation *= Quaternion.AngleAxis(Time.deltaTime * speed, Vector3.up);
    }
}
```

However MonoBehaviour inherits from a number of other classes; each containing their own set of data - none of which are in use by the script above. Therefore we have just wasted a bunch of memory for no good reason. So we need to think about what data we really need to optimize the code. 

### ComponentSystem - A step into a new era

In the new model the component only contains the data.

The __ComponentSystem__ contains the behavior. One ComponentSystem is responsible for updating all GameObjects with a matching set of components.

```C#
using Unity.Entities;
using UnityEngine;

class Rotator : MonoBehaviour
{
    // The data - editable in the inspector
    public float Speed;
}

class RotatorSystem : ComponentSystem
{
    override protected void OnUpdate()
    {
        // We can immediately see a first optimization.
        // We know delta time is the same between all rotators,
        // so we can simply keep it in a local variable 
        // to get better performance.
        float deltaTime = Time.deltaTime;
        
        // ComponentSystem.ForEach
        // lets us efficiently iterate over all GameObjects
        // that have both a Transform & Rotator components 
        ForEach((Transform transform, Rotator rotator) =>
        {
            transform.rotation *= Quaternion.AngleAxis(rotator.Speed * deltaTime, Vector3.up);
        });
    }
}
```

## Hybrid ECS: Using ComponentSystem to work with existing GameObject & components

There is a lot of existing code based on MonoBehaviour, GameObject and friends. We want to make it easy to work with existing GameObjects and existing components. But make it easy to transition one piece at a time to the ComponentSystem style approach.

In the example above you can see that we simply iterate over all components that contain both Rotator and Transform components.

### How does the Component System know about Rotator and Transform?

In order to iterate over components like in the Rotator example, those entities have to be known to the __EntityManager__.

ECS ships with the __GameObjectEntity__ component. On __OnEnable__, the GameObjectEntity component creates an entity with all components on the GameObject. As a result the full GameObject and all its components are now iterable by ComponentSystems.

> Thus for the time being you must add a GameObjectEntity component on each GameObject that you want to be visible / iterable from the ComponentSystem.

### How does the ComponentSystem get created?
Unity automatically creates a default world on startup and populates it with all Component Systems in the project. Thus if you have a game object with the the necessary components and a __GameObjectEntity__, the System will automatically start executing with those components.

### What does this mean for my game?

It means that you can one by one, convert behavior from __MonoBehaviour.Update__ methods into ComponentSystems. You can in fact keep all your data in a MonoBehaviour, and this is in fact a very simple way of starting the transition to ECS style code.

So your scene data remains in GameObjects & components. You continue to use [GameObject.Instantiate](https://docs.unity3d.com/ScriptReference/Object.Instantiate.html) to create instances etc.

You simply move the contents of your MonoBehaviour.Update into a __ComponentSystem.OnUpdate__ method. The data is kept in the same MonoBehaviour or other components.

#### What you get:

+ Separation of data & behavior resulting in cleaner code
+ Systems operate on many objects in batch, avoiding per object virtual calls. It is easy to apply optimizations in batch. (See deltaTime optimization above.)
+ You can continue to use existing inspectors, editor tools etc

#### What you don't get:

- Instantiation time will not improve
- Load time will not improve
- Data is accessed randomly, no linear memory access guarantees
- No multicore
- No [SIMD](https://en.wikipedia.org/wiki/SIMD)


So using ComponentSystem, GameObject and MonoBehaviour is a good first step to writing ECS code. It gives you some quick performance improvements, but it does not tap into the full range of performance benefits available.

## Pure ECS: Full-on performance - IComponentData & Jobs

One motivation to use ECS is because you want your game to have optimal performance. By optimal performance we mean that if you were to hand write all of your code using SIMD intrinsics (custom data layouts for each loop) then you would end up with similar performance to what you get when writing simple ECS code.

The C# job system does not support managed class types; only structs and __NativeContainers__. So only __IComponentData__ can be safely accessed in a C# Job.

The EntityManager makes hard guarantees about [linear memory layout](https://en.wikipedia.org/wiki/Flat_memory_model) of the component data. This is an important part of the great performance you can achieve with C# jobs using IComponentData.

```cs
using System;
using Unity.Entities;

// The rotation speed component simply stores the Speed value
[Serializable]
public struct RotationSpeed : IComponentData
{
    public float Value;
}

// This proxy component is currently necessary to add ComponentData to GameObjects.
public class RotationSpeedProxy : ComponentDataProxy<RotationSpeed> { } 
```

```cs
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Using IJobProcessComponentData to iterate over all entities matching the required component types.
// Processing of entities happens in parallel. The main thread only schedules jobs.
public class RotationSpeedSystem : JobComponentSystem
{
    // IJobProcessComponentData is a simple way of iterating over all entities given the set of required compoenent types.
    // By marking the job Burst compile we get significantly better codegen and thus much faster code at runtime.
    [BurstCompile]
    struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed>
    {
        public float dt;

        public void Execute(ref Rotation rotation, [ReadOnly]ref RotationSpeed speed)
        {
            rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.axisAngle(math.up(), speed.Value * dt));
        }
    }

    // We derive from JobComponentSystem, as a result the system proviides us 
    // the required dependencies for our jobs automatically.
    //
    // IJobProcessComponentData declares that it will read RotationSpeed and write to Rotation.
    //
    // Because it is declared the JobComponentSystem can give us a Job dependency, which contains all previously scheduled
    // jobs that write to any Rotation or RotationSpeed.
    // We also have to return the dependency so that any job we schedule 
    // will get registered against the types for the next System that might run.
    // This approach means:
    // * No waiting on main thread, just scheduling jobs with dependencies (Jobs only start when dependencies have completed)
    // * Dependencies are figured out automatically for us, so we can write modular multithreaded code
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new RotationSpeedRotation() { dt = Time.deltaTime };
        return job.Schedule(this, inputDeps);
    } 
}
```
