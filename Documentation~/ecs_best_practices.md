# Best Practices

As the ECS APIs change, so too will these best practices. For the time being, these are our best recommendations on how to use the ECS APIs.

### Dos
* Use IJobProcessComponentData for processing many entities' components in a job.
* Use chunk iteration if you need finer-grained control.

### Don'ts
* Do not use injection. It is deprecated.
* Do not use ComponentDataArray. It is deprecated.