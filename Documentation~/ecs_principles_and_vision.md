# Unity Data Oriented Tech Stack principles and vision

The Unity Data Oriented Tech Stack is built on a set of principles. These principles provide a good context for what we are trying to achieve. Some of the principles are clearly reflected in the code. Others are simply goals that we set for ourselves.

## Performance by default

We want to make it simple to create efficient machine code for all platforms.

We measure ourselves against the performance that can be achieved in C++ with handwritten highly optimized [simd](https://en.wikipedia.org/wiki/SIMD) intrinsics.

We are using a combination of compiler technology (Burst), containers (Unity.Collections), data layout of components (ECS) to make it easy to write efficient code by default.

* Data layout & iteration - The Entity Component System guarantees linear data layout when iterating entities in chunks by default. This is a critical part of the performance gains provided by the Data Oriented Tech Stack.
* The C# job system lets you write multithreaded code in a simple way. It is also safe. The C# Job Debugger detects any race conditions.
* Burst is our compiler specifically for C# jobs. C# job code follows certain patterns that we can use to produce more efficient machine code. Code is compiled & optimized for each target platforms taking advantage of SIMD instructions.

An example of this is the performance of Instantiation. Comparing to the theoretical limit, of instantiating 100,000 entities with 320 bytes of a memcpy takes 9ms. Instantiating those entities via the Entity Component System takes 10ms. So we are very close to the theoretical limit.

At Unite Austin, we showcased a demo with 100,000 individual units in a massive battle simulation running at 60 FPS. All game code was running multicore.
[See ECS performance demo [Video]](https://www.youtube.com/watch?v=0969LalB7vw)

## Simple

Writing [performant](https://en.wiktionary.org/wiki/performant) code must be simple. We believe that we can make writing fast code as simple as __MonoBehaviour.Update__. 

> Note: To set expectations right, we think we still have some ways to go to achieve this goal.

## One way of writing code

We want to define a single way of writing game code, editor code, asset pipeline code, engine code. We believe this creates a simpler tool for our users, and more ability to change things around.

Physics is a great example. Currently Physics is a black box solution. In practice many developers want to tweak the simulation code to fit it to their games needs. If physics engine code was written the same way as game code using ECS, it would make it easy to plug your own simulation code between existing physics simulation stages or take full control.

Another example, lets imagine you want to make a heavily moddable game.

If our import pipeline is implemented as a set of __ComponentSystems__. And we have some FBX import pipeline code that is by default used in the asset pipeline to import and postprocess an FBX file. (Mesh is baked out and FBX import code used in the editor.)

Then it would be easy to configure the Package Manager that the same FBX import and postprocessing code could be used in a deployed game for the purposes of modding.

We believe this will, at the foundation level, make Unity significantly more flexible than it is today.

## Networking

We want to define one simple way of writing all game code. When following this approach, your game can use one of three network architectures depending on what type of game you create.

We are focused on providing best of class network engine support for hosted games. Using the recently acquired [Multiplay.com](http://Multiplay.com) service we offer a simple pipeline to host said games.

* FPS - Simulation on the server
* RTS - Deterministic lock step simulation
* Arcade games - GGPO

> Note: To set expectations right, we are not yet shipping any networking code on top of Entity Component System. It is work in progress.

## Determinism

Our build pipeline must be [deterministic](https://en.wikipedia.org/wiki/Deterministic_algorithm). Users can choose if all simulation code should run deterministically.

You should always get the same results with the same inputs, no matter what device is being used. This is important for networking, replay features and even advanced debugging tools.

To do this, we will leverage our Burst compiler to produce exact floating point math between different platforms. Imagine a linux server & iOS device running the same floating point math code. This is useful for many scenarios particularly for connected games, but also debugging, replay etc. 

> Note: Floating point math discrepancies is a problem that Unity decided to tackle head on. This issue has been known about for some time, but so far there has not been a need great enough to encourage people to solve it. For some insight into this problem, including some of the workarounds needed to avoid solving it, consider reading [Floating-Point Determinism by Bruce Dawson](https://randomascii.wordpress.com/2013/07/16/floating-point-determinism/).

## Sandbox

Unity is a sandbox, safe and simple.

We provide great error messages when API's are used incorrectly, we never put ourselves in a position where incorrect usage results in a crash and that is by design (as opposed to a bug we can quickly fix).

A good example of sandbox behaviour is that our C# job system guarantees that none of your C# job code has race conditions. We deterministically check all possible race conditions through a combination of static code analysis & runtime checks. We give you well written error messages about any race conditions right away. So you can trust that your code works and feel safe that even developers who write multithreaded game code for the first time will do it right.

## Tiny

We want Unity to be usable for all content from < 50kb executables + content, to gigabyte sized games. We want Unity to load in less than 1 second for small content.

## Iteration time

We aim to keep iteration time for any common operations in a large project folder below 500ms.

As an example we are working on rewriting the C# compiler to be fully incremental with the goal of:

> When changing a single .cs file in a large project. The combined compile and hot reload time should be less than 500ms.

## Our code comes with full unit test coverage

We believe in shipping robust code from the start. We use unit tests to prove that our code works correctly when it is written and committed by the developer. Tests are shipped as part of the packages.

## Evolution

We are aware that we are proposing a rather large change in how to write code. From MonoBehaviour.Update to ComponentSystem & using jobs.

We believe that ultimately the only thing that convinces a game developer is trying it and seeing the result with your own eyes, on your own game. 

Thus it is important that applying the ECS approach on an existing project should be easy and quick to do. Our goal is that within 30 minutes a user can, in a large project, change some code from MonoBehaviour.Update to ComponentSystem and have a successful experience optimizing their game code.

## Packages

We want the majority of our engine code to be written in C# and deployed in a Package. All source code is available to all Unity Pro customers.

We want a rapid feedback loop with customers, given that we can push code and get feedback on something quickly in a package without destabilizing other parts.

Previously most of our engine code was written in C++, which creates a disconnect with how our customers write code and how programmers at Unity write code. Due to the Burst compiler tech & ECS, we can achieve better than C++ with C# code and as a result we can all write code exactly the same way.

## Collaboration

We believe Unity users and Unity developers are all on the same team. Our purpose is to help all Unity users create the best game experiences faster, in higher quality, and with great performance. 

We believe every feature we develop must be developed with real scenarios and real production feedback early on. The Package Manager facilitates that.

For those in the community that want to contribute engine code, we aim to make that easy by working directly on the same code repositories that contributors can commit to as well. Through well defined principles and full test coverage of all features, we hope to keep the quality of contributions high as well. 

The source code repositories will be available for all Unity Pro Customers.

## Transparency

We believe in transparency. We develop our features in the open, we actively communicate on both forum and blogs. We reserve time so each developer can spend time with customers and understand our users pain points.

