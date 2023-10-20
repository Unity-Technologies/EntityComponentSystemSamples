# DOTS Samples

- [Entities samples](./EntitiesSamples/Assets/README.md)
- [Netcode samples](./NetcodeSamples/Assets/README.md)
- [Physics samples](./PhysicsSamples/README.md)
- [Entities.Graphics HDRP samples](./GraphicsSamples/HDRPSamples/README.md)
- [Entities.Graphics URP samples](./GraphicsSamples/URPSamples/README.md)

# Learning DOTS

For those new to DOTS, here's the recommended sequence to follow through the introductory material in the [Entities samples project](./EntitiesSamples/Assets/README.md):

A few short videos introduce the basic concepts of the job system and ECS:

1. [Video: The C# Job system](https://youtu.be/jdW66hA-Qu8) (11 minutes)
1. [Video: ECS Entities and components](https://youtu.be/jzCEzNoztzM) (10 minutes)
1. [Video: ECS Systems](https://youtu.be/k07I-DpCcvE) (7 minutes)
1. [Video: ECS Baking](https://youtu.be/r337nXZFYeA) (6 minutes)

You may also want to read the [Entities API overview](./EntitiesSamples/Assets/README.md#entities-api-overview), which is briefer and more sequentially structured than the [manual](https://docs.unity3d.com/Packages/com.unity.entities@latest/).

These starter samples each have an explanatory video:

- The [Jobs Tutorial sample](./EntitiesSamples/Assets/Tutorials/Jobs/README.md) (17 minute [walkthrough video](https://youtu.be/oOgNg2gL2yw)) demonstrates creation and scheduling of jobs.
- The [HelloCube samples](./EntitiesSamples/Assets/HelloCube/README.md) (30 minute [walkthrough video](https://youtu.be/32TLgtA9yUM)) demonstrate very basic Entities usage, such as creating and moving rendered entities in systems and jobs.
- The [Tanks tutorial](./EntitiesSamples/Assets/Tutorials/Tanks/README.md) (23 minute [walkthrough video](https://youtu.be/jAVVxoWU5lo)) puts the basic elements of Entities and jobs together to demonstrate a small simulation.
- The [Kickball tutorial](./EntitiesSamples/Assets/Tutorials/Kickball/README.md) (55 minute [walkthrough video](https://youtu.be/P6_3L7RTcm0)) also demonstrates a small simulation, but with a bit more depth. 
- The [StateChange sample](./EntitiesSamples/Assets/Miscellaneous/StateChange/) (14 minute [walkthrough video](https://youtu.be/KC-EyCh5TrY)) demonstrates three different ways to handle state representation in Entities. 

Beyond the above starter samples, there are samples covering [Baking](./EntitiesSamples/Assets/Baking/README.md), [Streaming](./EntitiesSamples/Assets/Streaming/README.md) (for large worlds and scene management), and [Miscellaneous](./EntitiesSamples/Assets/Miscellaneous/README.md).

For quick reference of basic API usage, use these example code snippets and cheat sheets:

- [Example code: jobs](./EntitiesSamples/Assets/ExampleCode/Jobs.cs)
- [Example code: components and systems](./EntitiesSamples/Assets/ExampleCode/ComponentsSystems.cs)
- [Example code: baking](./EntitiesSamples/Assets/ExampleCode/Baking.cs)
- [Cheat sheet: collections](./EntitiesSamples/Docs/cheatsheet/collections.md)
- [Cheat sheet: mathematics](./EntitiesSamples/Docs/cheatsheet/mathematics.md)

Finally, there's the [ECS Network Racing sample](https://github.com/Unity-Technologies/ECS-Network-Racing-Sample), which is a working DOTS game using DOTS Netcode and Physics.

# Release notes

This is the samples release for Unity 2022.3 LTS and the 1.0 release of the `Entities`, `Netcode`, `Physics`, and `Entities.Graphics` packages.