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

## Community

* [Performance by Default](http://unity3d.com/performance-by-default)

## Further information

![Unity at GDC 2018](https://blogs.unity3d.com/wp-content/uploads/2018/03/Unity-GDC-Google-Desktop-Profile-Cover.jpg)

### Unity at GDC 2018

* [Keynote: The future of Unity (Entity Component System & Performance) by Joachim Ante](https://www.youtube.com/watch?v=3Mq9EH8RT_U)

* [Evolving Unity by Joachim Ante [FULL VIDEO]](https://www.youtube.com/watch?v=aFFLEiDr3T0)

* [Unity Job System and Entity Component System by Tim Johansson [FULL VIDEO]](https://www.youtube.com/watch?v=kwnb9Clh2Is)

* [Democratizing Data-Oriented Design: A Data-Oriented Approach to Using Component Systems by Mike Acton [FULL VIDEO]](https://www.youtube.com/watch?v=p65Yt20pw0g)

* [C# Sharp to Machine Code by Andreas Fredriksson [FULL VIDEO]](https://www.youtube.com/watch?v=NF6kcNS6U80)

* [ECS for Small Things by Vladimir Vukicevic [FULL VIDEO]](https://www.youtube.com/watch?v=EWVU6cFdmr0)

* [Exclusive: Unity takes a principled step into triple-A performance at GDC](https://www.mcvuk.com/development/exclusive-unity-takes-a-principled-step-into-triple-a-performance-at-gdc)

  ​

![Unity at Unite Austin 2017](https://blogs.unity3d.com/wp-content/uploads/2017/09/Unite_Austin_Blog_Post.jpg)

### Unity at Unite Austin 2017

* [Keynote: Performance demo ft. Nordeus by Joachim Ante](http://www.youtube.com/watch?v=0969LalB7vw)
* [Unity GitHub repository of Nordeus demo](https://github.com/Unity-Technologies/UniteAustinTechnicalPresentation)
* [Writing high performance C# scripts by Joachim Ante [FULL VIDEO]](http://www.youtube.com/watch?v=tGmnZdY5Y-E)



---

## Status of ECS

Entity iteration:
* We have implemented various approaches (foreach vs arrays, injection vs API). Right now we expose all possible ways of doing it, so that users can give us feedback on which one they like by actually trying them. Later on we will decide on the best way and delete all others.

Job API using ECS:
* We believe we can make it significantly simpler. The next thing to try out is Async / Await and see if there are some nice patterns that are both fast & simple.

Our goal is to be able to make entities editable just like GameObjects are. Scenes are either full of Entities or full of GameObjects. Right now we have no tooling for editing Entities without GameObjects. So in the future we want to:
* Display & edit Entities in Hierarchy window and Inspector window.
* Save Scene / Open Scene / Prefabs for Entities.

