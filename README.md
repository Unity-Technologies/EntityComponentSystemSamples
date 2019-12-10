# Welcome

Welcome to the DOTS Samples repository!
Here you can find the resources required to start building with these new systems today.
We have also provided a forum where you can find more information and share your experiences with these new systems.

[Click here to visit the forum](https://forum.unity.com/forums/data-oriented-technology-stack.147/)


## What is the Unity Data Oriented Tech Stack?

We have been working on a new high performance multithreaded system, that will make it possible for games to fully utilise the multicore processors available today without heavy programming headache. The Data Oriented Tech Stack includes the following major systems:

* The **Entity Component System** provides a way to write performant code by default.
* The **C# Job System** provides a way to run your game code in parallel on multiple CPU cores
* The **Burst compiler** a new math-aware, backend compiler tuned to produce highly optimized machine code.

With these systems, Unity can produce highly optimised code for the particular capabilities of the platform you’re compiling for.

For more information about the Unity DOTS initiative, see [Performance by Default](https://unity3d.com/performance-by-default) on the Unity website.


## Entity Component System

The Entity Component System offers a better approach to game design that allows you to concentrate on the actual problems you are solving: the data and behavior that make up your game. It leverages the C# Job System and Burst Compiler enabling you to take full advantage of today's multicore processors. Moving from object-oriented to data-oriented design makes it easier for you to reuse the code and easier for others to understand and work on it.

The Entity Component System ships as an experimental package that currently supports Unity 2018.3 and later. It is important to stress that the Entity Component System is not production ready.


## C# Job System

The new C# Job System takes advantage of multiple cores in a safe and easy way. Easy, as it’s designed to open this approach up to user scripts and allows you to write safe, fast, jobified code while providing protection from some of the pitfalls of multi-threading such as race conditions.

The C# Job System is a built-in module included in Unity 2018.1+.


## Burst

Burst is a new LLVM-based, math-aware backend compiler. It compiles C# jobs into highly-optimized machine code that takes advantage of the particular capabilities of the platform you’re compiling for. Burst is an experimental package that currently supports Unity 2018.4 and later. 

[Watch Joachim Ante present these systems at Unite Austin](https://youtu.be/tGmnZdY5Y-E)


## Installation guide

1. Create or open a project with the Unity Editor (`2019.3.0b11` or later to use the latest DOTS packages).
2. Open the Package Manager (menu: **Window** > **Package Manager**).
3. Open the **Advanced** menu at the top of the window and turn on the **Show preview packages** option.
4. Add the **Hybrid Renderer** package to the project.

This will add the **Entities** package as a dependency which will add the related packages it depends on recursively (e.g. **Burst**, **Collections**, **Jobs** and **Mathematics**). Now you can start to use DOTS in you project.

**Note:** You can use the [Unity Hub](https://unity3d.com/get-unity/download) to install multiple versions of Unity on the same computer.


## ECS Samples

The ECSSamples project contains samples to help you get started with DOTS. The project [release notes](ECSSamples/ReleaseNotes.md) contain a summary of changes.


## Physics Samples

Unity Physics is a deterministic, rigid-body dynamics system and spatial query system written from the ground up using the Unity data oriented tech stack.
You can find an [overview of the samples](UnityPhysicsSamples/Documentation/samples.md) and the [release notes](UnityPhysicsSamples/ReleaseNotes.md) in this repository.


## Documentation

Looking for information on how to get started or have specific questions? Visit our ECS & Job system documentation.

* [ECS](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html)
* [Burst](https://docs.unity3d.com/Packages/com.unity.burst@latest/index.html)
* [C# Jobs](https://docs.unity3d.com/Manual/JobSystem.html)
* [Unity.Mathematics](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html)

Note that from the Unity Editor, you can access the documentation for any package from the Package Manager window.
