![Unity](https://unity3d.com/files/images/ogimg.jpg?1)
# Entity-component-system user manual

* [ECS principles](content/ecs_principles_and_vision.md)
* [Is ECS for you?](content/is_ecs_for_you.md)
* [ECS concepts](content/ecs_concepts.md)
* [How ECS works](content/getting_started.md)
* [ECS features in detail](content/ecs_in_detail.md)
* [Further information](#further-information)
* [Status of ECS](#status-of-ecs)

## Job system overview

* [How the job system works](content/job_system.md)
* [Low-level overview - creating containers & custom job types](content/custom_job_types.md)
* [How to optimize for the Burst compiler](content/burst_optimization.md)
* [Scheduling a job from a job - why not?](content/scheduling_a_job_from_a_job.md)

## Tutorials

* [Tutorial walk through: A two-stick shooter in ECS](content/two_stick_shooter.md)

## Simple examples

* [RotationExample.unity](content/rotation_example.md): Loop to change component if Entity position is inside a moving sphere.

## Further information

* [*MCV 2018 - Exclusive: Unity takes a principled step into triple-A performance at GDC*](https://www.mcvuk.com/development/exclusive-unity-takes-a-principled-step-into-triple-a-performance-at-gdc)

*Unite Austin 2017 keynote - Performance demo ft. Nordeus*
[![Unite Austin 2017 keynote - Performance demo ft. Nordeus](http://img.youtube.com/vi/0969LalB7vw/0.jpg)](http://www.youtube.com/watch?v=0969LalB7vw)

*Unite Austin 2017 - Writing high performance C# scripts*
[![Unite Austin 2017 - Writing high performance C# scripts](http://img.youtube.com/vi/tGmnZdY5Y-E/0.jpg)](http://www.youtube.com/watch?v=tGmnZdY5Y-E)

---

## Status of ECS

Entity iteration:
* We have implemented various approaches (foreach vs arrays, injection vs API). Right now we expose all possible ways of doing it, so that users can give us feedback on which one they like by actually trying them. Later on we will decide on the best way and delete all others.

Job API using ECS:
* We believe we can make it significantly simpler. The next thing to try out is Async / Await and see if there are some nice patterns that are both fast & simple.

Our goal is to be able to make entities editable just like GameObjects are. Scenes are either full of Entities or full of GameObjects. Right now we have no tooling for editing Entities without GameObjects. So in the future we want to:
* Display & edit Entities in Hierarchy window and Inspector window.
* Save Scene / Open Scene / Prefabs for Entities.

