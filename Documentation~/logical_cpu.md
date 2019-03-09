# Logical CPU

General computing term. Also known as logical processors.

> [Directions on Microsoft](https://www.directionsonmicrosoft.com/licensing/30-licensing/3420-sql-server-2012-adopts-per-core-licensing-model.html): "Logical processors subdivide a server's processing power to enable parallel processing." 

> [Unix Stack Exchange](https://unix.stackexchange.com/questions/88283/so-what-are-logical-cpu-cores-as-opposed-to-physical-cpu-cores): "Physical cores are [the] number of physical cores, actual hardware components. Logical cores are the number of physical cores times the number of threads that can run on each core through the use of hyperthreading. For example, my 4-core processor runs two threads per core, so I have 8 logical processors." 

> [How-To Geek](https://www.howtogeek.com/194756/cpu-basics-multiple-cpus-cores-and-hyper-threading-explained/): "A single physical CPU core with hyper-threading appears as two logical CPUs to an operating system. The CPU is still a single CPU, so it’s a little bit of a cheat. While the operating system sees two CPUs for each core, the actual CPU hardware only has a single set of execution resources for each core. The CPU pretends it has more cores than it does, and it uses its own logic to speed up program execution. In other words, the operating system is tricked into seeing two CPUs for each actual CPU core. Hyper-threading allows the two logical CPU cores to share physical execution resources. This can speed things up somewhat—if one virtual CPU is stalled and waiting, the other virtual CPU can borrow its execution resources. Hyper-threading can help speed your system up, but it’s nowhere near as good as having actual additional cores."

See also: [multithreading](https://docs.unity3d.com/Manual/JobSystemMultithreading.html) and [multicore](multicore.md).

[Back to Unity Data-Oriented reference](reference.md)