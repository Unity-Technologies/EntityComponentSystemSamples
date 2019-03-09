# Atomic operation

General computing term. Also known as linearizable or uninterruptible. 

> [Wikipedia](https://en.wikipedia.org/wiki/Linearizability): "In concurrent programming, an operation (or set of operations) is atomic, linearizable, indivisible or uninterruptible if it appears to the rest of the system to occur at once without being interrupted. Atomicity is a guarantee of isolation from interrupts, signals, concurrent processes and threads… Additionally, atomic operations commonly have a succeed-or-fail definition—they either successfully change the state of the system, or have no apparent effect."

> [Preshing on Programming](http://preshing.com/20130618/atomic-vs-non-atomic-operations/): "An operation acting on shared memory is atomic if it completes in a single step relative to other threads. When an atomic store is performed on a shared variable, no other thread can observe the modification half-complete. When an atomic load is performed on a shared variable, it reads the entire value as it appeared at a single moment in time… Any time two threads operate on a shared variable concurrently, and one of those operations performs a write, both threads must use atomic operations."

[Back to Unity Data-Oriented reference](reference.md)