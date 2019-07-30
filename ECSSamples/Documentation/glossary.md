# Glossary

This section defines specific and general computing terminology relevant to the Unity Data Oriented Tech Stack.

<a name="aot_compilation"></a>
## AOT compilation

AOT stands for "Ahead-of-time". 

> [Wikipedia](https://en.wikipedia.org/wiki/Ahead-of-time_compilation): "In computer science, ahead-of-time (AOT) compilation is the act of compiling a higher-level programming language such as C or C++, or an intermediate representation such as Java bytecode or .NET Framework Common Intermediate Language (CIL) code, into a native (system-dependent) machine code so that the resulting binary file can execute natively. AOT produces machine optimized code, just like a standard native compiler. The difference is that AOT transforms the bytecode of an extant virtual machine (VM) into machine code."

See also: [JIT compilation](#jit_compilation).

<a name="atomic_operation"></a>
## Atomic operation

Also known as linearizable or uninterruptible. 

> [Wikipedia](https://en.wikipedia.org/wiki/Linearizability): "In concurrent programming, an operation (or set of operations) is atomic, linearizable, indivisible or uninterruptible if it appears to the rest of the system to occur at once without being interrupted. Atomicity is a guarantee of isolation from interrupts, signals, concurrent processes and threads… Additionally, atomic operations commonly have a succeed-or-fail definition—they either successfully change the state of the system, or have no apparent effect."

> [Preshing on Programming](http://preshing.com/20130618/atomic-vs-non-atomic-operations/): "An operation acting on shared memory is atomic if it completes in a single step relative to other threads. When an atomic store is performed on a shared variable, no other thread can observe the modification half-complete. When an atomic load is performed on a shared variable, it reads the entire value as it appeared at a single moment in time… Any time two threads operate on a shared variable concurrently, and one of those operations performs a write, both threads must use atomic operations."

<a name="blittable_types"></a>
## Blittable types

> [Wikipedia](https://en.wikipedia.org/wiki/Blittable_types): "Blittable types are data types in the Microsoft .NET framework that have an identical presentation in memory for both managed and unmanaged code… A memory copy operation is sometimes referred to as a 'block transfer'. This term is sometimes abbreviated as BLT... and pronounced 'blit'. The term 'blittable' expresses whether it is legal to copy an object using a block transfer."

See also: [managed code](#managed_code) and [unmanged code](#unmanaged_code).

<a name="burst_compiler"></a>
## Burst compiler

Burst is a new [LLVM](https://en.wikipedia.org/wiki/LLVM) based backend compiler technology that makes things easier for you. It takes C# jobs and produces highly-optimized machine code taking advantage of the particular capabilities of your platform. So you get a lot of the benefits of hand tuned assembler code, across multiple platforms, without all the hard work. The Burst compiler can be used to increase performance of jobs written for the [C# Job System](https://docs.unity3d.com/Manual/JobSystem.html). 

<a name="dependency"></a>
## Dependency

Also known as coupling.

> [Jenkov](http://tutorials.jenkov.com/ood/understanding-dependencies.html): "Whenever a class **A** uses another class… **B**, then **A** depends on **B**. **A** cannot carry out it's work without **B**, and **A** cannot be reused without also reusing **B**. In such a situation the class **A** is called the 'dependant' and the class… **B** is called the 'dependency'. A dependant depends on its dependencies."

In the context of the Data-Oriented Tech Stack, the Job system allows you to establish dependencies between Jobs. The Job system scheduler ensures only executes a job once all the jobs it depends upon have finished.


<a name="ecs"></a>
## ECS

An [entity-component-system](https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system) (ECS) is a new model to write performant code by default. Instead of using [Object-Oriented Design](https://en.wikipedia.org/wiki/Object-oriented_design) (OOD), ECS takes advantage of another paradigm called [Data-Oriented Design](https://en.wikipedia.org/wiki/Data-oriented_design). This  separates out the data from the logic so you can apply instructions to a large batch of items in parallel. The Entity-component-system gurantees [linear data layout](https://en.wikipedia.org/wiki/Flat_memory_model) when iterating over entities in [chunks](com.unity.entities/chunk_iteration.md). Managing data this way is quicker because you read from continuous blocks of memory, rather than random blocks assigned all over the place. Knowing exactly where each bit of data is, and by packing it tightly together, allows us to manage memory with little overhead. This is a critical part of the performance gains provided by ECS.

>  Note: Unity's ECS is a fairly standard entity-component-system, although the naming is tweaked somewhat to avoid clashes with existing concepts within Unity. (See [ECS concepts](com.unity.entities/ecs_core.md) for more information.)

See also: [Entity](com.unity.entities/ecs_entities.md), [ComponentData](com.unity.entities/component_data.md), and [ComponentSystem](com.unity.entities/component_system.md). 

<a name="jit_compilation"></a>
## JIT compilation

JIT stands for "Just-in-time". 

> [Wikipedia](https://en.wikipedia.org/wiki/Just-in-time_compilation): "In computing, just-in-time (JIT) compilation, also known as dynamic translation, is a way of executing computer code that involves compilation during execution of a program – at run time – rather than prior to execution."

See also: [AOT compilation](#aot_compilation)

<a name="logical_cpu"></a>
## Logical CPU

Also known as a logical processor.

> [Directions on Microsoft](https://www.directionsonmicrosoft.com/licensing/30-licensing/3420-sql-server-2012-adopts-per-core-licensing-model.html): "Logical processors subdivide a server's processing power to enable parallel processing." 

> [Unix Stack Exchange](https://unix.stackexchange.com/questions/88283/so-what-are-logical-cpu-cores-as-opposed-to-physical-cpu-cores): "Physical cores are [the] number of physical cores, actual hardware components. Logical cores are the number of physical cores times the number of threads that can run on each core through the use of hyperthreading. For example, my 4-core processor runs two threads per core, so I have 8 logical processors." 

> [How-To Geek](https://www.howtogeek.com/194756/cpu-basics-multiple-cpus-cores-and-hyper-threading-explained/): "A single physical CPU core with hyper-threading appears as two logical CPUs to an operating system. The CPU is still a single CPU, so it’s a little bit of a cheat. While the operating system sees two CPUs for each core, the actual CPU hardware only has a single set of execution resources for each core. The CPU pretends it has more cores than it does, and it uses its own logic to speed up program execution. In other words, the operating system is tricked into seeing two CPUs for each actual CPU core. Hyper-threading allows the two logical CPU cores to share physical execution resources. This can speed things up somewhat—if one virtual CPU is stalled and waiting, the other virtual CPU can borrow its execution resources. Hyper-threading can help speed your system up, but it’s nowhere near as good as having actual additional cores."

See also: [Multithreading](https://docs.unity3d.com/Manual/JobSystemMultithreading.html) and [Multicore](#multicore).
.
<a name="main_thread"></a>
## Main thread

> [Geeks for Geeks](https://www.geeksforgeeks.org/main-thread-java/): "When a ...program starts up, one thread begins running immediately. This is usually called the **main** thread of our program, because it is the one that is executed when our program begins.
>
> **Properties:**
>
> - It is the thread from which other “child” threads will be spawned.
> - Often, it must be the last thread to finish execution because it performs various shutdown actions."

Many programming languages have a Main method that is the starting point of an application. The main thread will find this method and invoke it. Your program will run on the main thread unless you create additional threads yourself. For more information, see [Stackoverflow](https://stackoverflow.com/questions/17669159/what-is-the-relation-between-the-main-method-and-main-thread-in-java). 

>  Note: In ECS we aim to remove as much code as possible out of the main thread and into jobs.

See also: [Multithreading](https://docs.unity3d.com/Manual/JobSystemMultithreading.html) and [Worker threads](#worker_threads).

<a name="managed_code"></a>
## Managed code


> [Wikipedia](https://en.wikipedia.org/wiki/Managed_code): "Managed code is computer program code that requires and will execute only under the management of a Common Language Runtime virtual machine, typically the .NET Framework, or Mono. The term was coined by Microsoft.
>
> Managed code is the compiler output of source code written in one of over twenty high-level programming languages that are available for use with the Microsoft .NET Framework, including C#, J#, Microsoft Visual Basic .NET, Microsoft JScript and .NET...
>
> Managed code in the Microsoft .Net Framework is defined according to the Common Intermediate Language specification.

See also: [unmanaged code](#unmanaged_code).

<a name="memory_leak"></a>
## Memory leak

General computing term.

> [Wikipedia](https://en.wikipedia.org/wiki/Memory_leak): "In computer science, a memory leak is a type of resource leak that occurs when a computer program incorrectly manages memory allocations in such a way that memory which is no longer needed is not released. In object-oriented programming, a memory leak may happen when an object is stored in memory but cannot be accessed by the running code. A memory leak has symptoms similar to a number of other problems and generally can only be diagnosed by a programmer with access to the program's source code… A memory leak reduces the performance of the computer by reducing the amount of available memory. Eventually, in the worst case, too much of the available memory may become allocated and all or part of the system or device stops working correctly, the application fails, or the system slows down vastly…"

See also: [DisposeSentinel](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html) - see "NativeContainer and the safety system."

<a name="multicore"></a>
## Multicore

> [Wikipedia](https://en.wikipedia.org/wiki/Multi-core_processor): "A multi-core processor is a single computing component with two or more independent processing units called cores, which read and execute program instructions. The instructions are ordinary CPU instructions... but the single processor can run multiple instructions on separate cores at the same time, increasing overall speed for programs amenable to parallel computing."

See also: [logical CPU](#logical_cpu).

<a name="performant"></a>
## Performant

> [Wiktionary](https://en.wiktionary.org/wiki/performant): "Capable of achieving an adequate or excellent level of performance or efficiency." 

> [Techopedia](https://www.techopedia.com/definition/28231/performant): "Performant means that something is working correctly or well enough to be considered functional. In a technology context, this term is believed to have originated with programmers seeking a concise word to express that a system or program will work, but may not yet be optimal. Performant may have come from a portmanteau of performance and conformant - as in working and meeting existing standards."

> [Stackoverflow](https://stackoverflow.com/questions/2112743/what-does-performant-software-actually-mean): "Performant is a word that was made up by software developers to describe software that performs well, in whatever way you want to define performance."

<a name="simd"></a>
## SIMD

SIMD stands for "Single Instruction Multiple Data". 

> [Wikipedia](https://en.wikipedia.org/wiki/SIMD): "SIMD… describes computers with multiple processing elements that perform the same operation on multiple data points simultaneously. Thus, such machines exploit [data level parallelism](https://en.wikipedia.org/wiki/Data_parallelism), but not [concurrency](https://en.wikipedia.org/wiki/Concurrent_computing): there are simultaneous (parallel) computations, but only a single process (instruction) at a given moment… In other words, if the SIMD system works by loading up eight data points at once, the `add` operation being applied to the data will happen to all eight values at the same time.  

See also: [ParallelFor jobs](https://docs.unity3d.com/Manual/JobSystemParallelForJobs.html).

<a name="unmanaged_code"></a>
## Unmanaged code 

Also known as "native" code.

> [Wikipedia](https://en.wikipedia.org/wiki/Managed_code): "...Unmanaged code refers to programs written in C, C++, and other languages that do not need a [common language] runtime [virtual machine] to execute."

> [Stackoverflow](https://stackoverflow.com/questions/855756/difference-between-native-and-managed-code?answertab=votes#tab-top): "Native code is the code whose memory is not 'managed', as in, memory isn't freed for you (C++' delete and C's free, for instance), no [reference counting](https://en.wikipedia.org/wiki/Reference_counting), no [garbage collection](https://en.wikipedia.org/wiki/Garbage_collection_(computer_science))."

See also: [managed code](#managed_code).

<a name="worker_threads"></a>
## Worker threads

Also known as thread pooling.

> [Wikipedia](https://en.wikipedia.org/wiki/Thread_(computing)#Multithreading): "By moving such long-running tasks to a worker thread that runs concurrently with the main execution thread, it is possible for the application to remain responsive to user input while executing tasks in the background."

> [Wikipedia](https://en.wikipedia.org/wiki/Thread_pool): "…A thread pool maintains multiple threads waiting for tasks to be allocated for concurrent execution by the supervising program. By maintaining a pool of threads, the model increases performance and avoids latency in execution due to frequent creation and destruction of threads for short-lived tasks. The number of available threads is tuned to the computing resources available to the program, such as parallel processors, cores, memory, and network sockets."

See also: [multithreading](https://docs.unity3d.com/Manual/JobSystemMultithreading.html), [multicore](#multicore), and [main thread](#main_thread).
