# Welcome
Welcome to the DOTS Samples repository!

Here you can find the resources required to start building with these new systems today.

We have also provided a forum where you can find more information and share your experiences with these new systems.

[Click here to visit the forum](https://unity3d.com/performance-by-default)

## What is the Unity Data Oriented Tech Stack?
We have been working on a new high performance multithreaded system, that will make it possible for games to fully utilise the multicore processors available today without heavy programming headache. The Data Oriented Tech Stack includes the following major systems:

* The **Entity Component System** provides a way to write performant code by default. 
* The **C# Job System** provides a way to run your game code in parallel on multiple CPU cores
* The **Burst compiler** a new math-aware, backend compiler tuned to produce highly optimized machine code.

With these systems, Unity can produce highly optimised code for the particular capabilities of the platform you’re compiling for. 

## Entity Component System
The Entity Component System offers a better approach to game design that allows you to concentrate on the actual problems you are solving: the data and behavior that make up your game. It leverages the C# Job System and Burst Compiler enabling you to take full advantage of today's multicore processors. Moving from object-oriented to data-oriented design makes it easier for you to reuse the code and easier for others to understand and work on it.

The Entity Component System ships as an experimental package that currently supports Unity 2018.3 and later. It is important to stress that the Entity Component System is not production ready.

## C# Job System
The new C# Job System takes advantage of multiple cores in a safe and easy way. Easy, as it’s designed to open this approach up to user scripts and allows you to write safe, fast, jobified code while providing protection from some of the pitfalls of multi-threading such as race conditions.

The C# Job System is a built-in module included in Unity 2018.1+.

[Further sample projects on the C# Job System can be found here](https://github.com/stella3d/job-system-cookbook)

## Burst
Burst is a new LLVM-based, math-aware backend compiler. It compiles C# jobs into highly-optimized machine code that takes advantage of the particular capabilities of the platform you’re compiling for.

Burst is an experimental package that currently supports Unity 2018.3 and later. It is important to stress that Burst is not production ready.

[Watch Joachim Ante present these new systems at Unite Austin](https://youtu.be/tGmnZdY5Y-E)

## Samples
To help you get started, we have provided this repository of examples for learning how to to write systems at scale. 

### HelloCube
This is a set of projects demonstrate the absolute basics of the Unity ECS architecture:

* **ForEach** — creates a pair of rotating cubes. This example demonstrates the separation of data and behavior with System and Components.
* **IJobForEach** — builds on the ForEach sample, using a Job-based system. Systems based on IJobForEach are the recommended approach and can take advantage of available CPU cores.
* **IJobChunk** — shows how to write a System using IJobChunk. IJobChunk is the recommended method for processing Components for cases more complex than a simple IJobForEach can describe.  
* **SubScene** — demonstrates how to create and modify Entities using SubScenes in the Unity editor.
* **SpawnFromMonoBehaviour** — demonstrates how to spawn multiple Entities from a MonoBehaviour function based on a Prefab GameObject.
* **SpawnFromEntity** — demonstrates how to spawn multiple Entities at runtime using a spawning Job in a system.
* **FluentQuery** — demonstrates how to use fluent queries to select the correct set of entities to update.
* **SpawnAndRemove** — demonstrates spawning and removing entities from the world.

### Boids

The Boids example provides a more complex scenario with thousands of Entities. Boids simulates an underwater scene with a shark and schools containing thousands of fish. (It uses the classic Boids flocking algorithm for the schooling fish behavior.)

## Installation guide for blank ECS project

1. Open the Unity Editor (`2019.1.0f1` or later)
2. Create a new Project.
3. Open the Package Manager (menu: **Window** > **Package Manager**).
4. Click the **Advanced** button at the top of the window and turn on the **Show preview packages** option.
5. Add the following packages to the project:

  * Entities
  * Hybrid.Renderer

  Adding the Entities package to your Project also adds the following packages:
  
  * Burst
  * Collections
  * Jobs
  * Mathematics 

**Note:** You can use the [Unity Hub](https://unity3d.com/get-unity/download) to install multiple versions of Unity on the same computer.

## Documentation
Looking for information on how to get started or have specific questions? Visit our ECS & Job system documentation.

[Go to documentation](Documentation~/index.md)

[Click here for the Physics samples Documentation](UnityPhysicsExamples/Documentation/samples.md)
