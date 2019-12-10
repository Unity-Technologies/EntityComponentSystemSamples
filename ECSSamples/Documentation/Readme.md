# Unity Data-Oriented Tech Stack

The Unity Data-Oriented Tech Stack has three main pieces: the Unity Entity-Component-System (ECS), the Unity C# Job System, and the Unity Burst compiler.

## Entity-Component-System overview

ECS offers an approach to game design that allows you to concentrate on the actual problems you are solving: the data and behavior that make up your game. ECS takes advantage of the C# Job System and the Burst compiler to fully utilize today's multicore processors. 

In addition to better utilizing modern CPUs, the [data-oriented](https://en.wikipedia.org/wiki/Data-oriented_design) design underlying ECS avoids the pitfalls of [object-oriented](https://simple.wikipedia.org/wiki/Object-oriented_programming) that can plague complex projects like games, especially when trying to eek out the last few FPS to reach your release target. Data-oriented design can also make it easier for you to reuse and evolve your code and for others to understand and work on it. For more information, see:

- [What is ECS?](getting_started.md)
- [ECS principles](ecs_principles_and_vision.md)
- [Is ECS for you?](is_ecs_for_you.md)
- [ECS Manual and Script Reference](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/manual/index.html)

## C# Job System overview

The C# Job System takes advantage of the multiple cores in today's computers. It’s designed to open this approach up to C# user scripts and allows users to write safe, fast, jobified code while protecting against some of the pitfalls of multithreading such as race conditions.

- [C# Job System manual](https://docs.unity3d.com/Manual/JobSystem.html)
- [Low-level overview - creating containers & custom job types](https://docs.unity3d.com/Packages/com.unity.jobs@latest?preview=1&subfolder=/manual/custom_job_types.html)
- [Scheduling a job from a job - why not?](https://docs.unity3d.com/Packages/com.unity.jobs@latest?preview=1&subfolder=/manual/scheduling_a_job_from_a_job.html)

## Burst overview

Burst is a new [LLVM](https://en.wikipedia.org/wiki/LLVM) based backend compiler technology that makes things easier for you. It takes C# jobs and produces highly-optimized machine code taking advantage of the particular capabilities of your platform. So you get a lot of the benefits of hand tuned assembler code, across multiple platforms, without all the hard work. The Burst compiler can be used to increase performance of jobs written for the C# Job System. 

- [Burst documentation](https://docs.unity3d.com/Packages/com.unity.burst@latest/index.html)
- [How to optimize for the Burst compiler](burst_optimization.md)

## Further information

- [Unity Data-Oriented cheat sheet](cheatsheet.md)
- [Unity Data-Oriented learning resources](resources.md)
- [Performance by default](http://unity3d.com/performance-by-default)
