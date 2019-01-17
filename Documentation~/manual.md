# Unity Data-Oriented Tech Stack manual

This manual covers the three main aspects of Unity's Data-Oriented Tech Stack: Unity Entity-Component-System (ECS) as covered by the Entities package, Unity C# Job System, and the Unity Burst compiler.

## Entity-Component-System overview

ECS offers a better approach to game design that allows you to concentrate on the actual problems you are solving: the data and behavior that make up your game. It leverages the [C# Job System](#c-job-system-overview) and the [Burst compiler](#burst-overview) enabling you to take full advantage of today's multicore processors. By moving from [object-oriented](https://simple.wikipedia.org/wiki/Object-oriented_programming) to [data-oriented](https://en.wikipedia.org/wiki/Data-oriented_design) design, it will be easier for you to reuse the code and for others to understand and work on it. For more information, see:

- [ECS principles](ecs_principles_and_vision.md)
- [Is ECS for you?](is_ecs_for_you.md)
- [ECS concepts](ecs_concepts.md)
- [ECS features in detail](ecs_in_detail.md)
- [ECS API best practices](ecs_best_practices.md)

## C# Job System overview

The new C# Job System takes advantage of the multiple cores in today's computers. It’s designed to open this approach up to C# user scripts and allows users to write safe, fast, jobified code while protecting against some of the pitfalls of multithreading such as race conditions.

- [C# Job System manual](https://docs.unity3d.com/Manual/JobSystem.html)
- [Low-level overview - creating containers & custom job types](custom_job_types.md)
- [Scheduling a job from a job - why not?](scheduling_a_job_from_a_job.md)

## Burst overview

Burst is a new [LLVM](https://en.wikipedia.org/wiki/LLVM) based backend compiler technology that makes things easier for you. It takes C# jobs and produces highly-optimized machine code taking advantage of the particular capabilities of your platform. So you get a lot of the benefits of hand tuned assembler code, across multiple platforms, without all the hard work. The Burst compiler can be used to increase performance of jobs written for the [C# Job System](#c-job-system-overview). 

- [How to optimize for the Burst compiler](burst_optimization.md)

## Further information

- [Unity Data-Oriented cheat sheet](cheatsheet.md)
- [Unity Data-Oriented reference](reference.md)
- [Unity Data-Oriented status](status.md)
- [Unity Data-Oriented resources](resources.md)
- [Performance by default forum](http://unity3d.com/performance-by-default)