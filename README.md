# Welcome
Welcome to the Entity Component System and C# Job System samples repository!

Here you can find the resources required to start building with these new systems today.

We have also provided a new forum where you can find more information and share your experiences with these new systems.

[Click here to visit the forum](https://unity3d.com/performance-by-default)

## What is in the build
We have been working on a new high performance multithreaded system, that will make it possible for games to fully utilise the multicore processors available today without heavy programming headache. This is possible thanks to the new Entity Component System which provides a way to write performant code by default. Paired with the C# Job System and a new math-aware backend compiler technology named Burst. Unity can produce highly optimised code for the particular capabilities of the platform you’re compiling for. 

[Download the beta build required here](https://beta.unity3d.com/download/ed1bf90b40e6/public_download.html)

## Entity Component System
Offers a better approach to game design  that allows you to concentrate on the actual problems you are solving: the data and behavior that make up your game. It leverages the C# Job System and Burst Compiler enabling you to take full advantage of today's multicore processors. By moving from object-oriented to data-oriented design it will be easier for you to reuse the code and easier for others to understand and work on it

The Entity Component System ships as an experimental package in 2018.1, and we’ll continue to develop and release new versions of the package in the 2018.x cycle. It is important to stress that the Entity Component System is not production ready 

## C# Job System
The new C# Job System takes advantage of multiple cores in a safe and easy way. Easy, as it’s designed to open this approach up to user scripts and allows users to write safe, fast, jobified code while providing protection from some of the pitfalls of multi-threading such as race conditions.

The C# Job System ships in 2018.1.

## Burst
Burst is a new LLVM based math-aware backend Compiler Technology makes things easier for you. It takes the C# jobs and produces highly-optimized code taking advantage of the particular capabilities of the platform you’re compiling for.

Burst ships as an experimental package in 2018.1, and we’ll continue to develop and release new versions of the package in the 2018.x cycle. For the current package release, Burst only works in the Unity editor. It is important to stress that Burst is not production ready

[Watch Joachim Ante present these new systems at Unite Austin](https://youtu.be/tGmnZdY5Y-E)

## Samples
To help you get started, we have provided this repository of examples for learning how to to write systems at scale. 

### The TwoStickShooter project
This is a set of projects that demonstrates different approaches with the MonoBehaviour, Hybrid Entity Component System and Pure Entity Component System. This is a good starting point to understand how the Entity Component System paradigm works. 

[Further sample projects on the C# Job System can be found here](https://github.com/stella3d/job-system-cookbook)

## Documentation
Looking for information on how to get started or have specific questions? Visit our ECS & Job system documentation 

[Go to documentation](https://github.com/Unity-Technologies/EntityComponentSystemSamples/blob/master/Documentation/index.md)
