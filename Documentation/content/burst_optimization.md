# How to optimize for the Burst compiler

* Use Unity.Mathematics, Burst natively understands the math operations and is optimized for it.
* Avoid branches. Use math.min, math.max, math.select instead.
* For jobs that have to be highly optimized, ensure that each job uses every single variable in the IComponentData. If some variables in an IComponentData is not being used, move it to a separate component. That way the unused data will not be loaded into cache lines when iterating over Entities.
