# JobComponentSystem

## Automatic job dependency management 

Managing dependencies is hard. This is why in `JobComponentSystem` we are doing it automatically for you. The rules are simple: jobs from different systems can read from `IComponentData` of the same type in parallel. If one of the jobs is writing to the data then they can't run in parallel and will be scheduled with a dependency between the jobs.

```cs
public class RotationSpeedSystem : JobComponentSystem
{
    [BurstCompile]
    struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed>
    {
        public float dt;

        public void Execute(ref Rotation rotation, [ReadOnly]ref RotationSpeed speed)
        {
            rotation.value = math.mul(math.normalize(rotation.value), quaternion.axisAngle(math.up(), speed.speed * dt));
        }
    }

    // Any previously scheduled jobs reading/writing from Rotation or writing to RotationSpeed 
    // will automatically be included in the inputDeps dependency.
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new RotationSpeedRotation() { dt = Time.deltaTime };
        return job.Schedule(this, inputDeps);
    } 
}
```

## How does this work?

All jobs and thus systems declare what ComponentTypes they read or write to. As a result when a JobComponentSystem returns a [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) it is automatically registered with the `EntityManager` and all the types including the information about if it is reading or writing.

Thus if a system writes to component `A`, and another system later on reads from component `A`, then the `JobComponentSystem` looks through the list of types it is reading from and thus passes you a dependency against the job from the first system.

`JobComponentSystem` simply chains jobs as dependencies where needed and thus causes no stalls on the main thread. But what happens if a non-job `ComponentSystem` accesses the same data? Because all access is declared, the `ComponentSystem` automatically completes all jobs running against component types that the system uses before invoking `OnUpdate`.

## Dependency management is conservative & deterministic

Dependency management is conservative. `ComponentSystem` simply tracks all `ComponentGroup`objects ever used and stores which types are being written or read based on that. (So if you inject an `ComponentDataArray` or use `IJobProcessComponentData` once but skip using it sometimes, then we will create dependencies against all `ComponentGroup` objects that have ever been used by that `ComponentSystem`.)

Also when scheduling multiple jobs in a single system, dependencies must be passed to all jobs even though different jobs may need less dependencies. If that proves to be a performance issue the best solution is to split a system into two.

The dependency management approach is conservative. It allows for deterministic and correct behaviour while providing a very simple API.

## Sync points

All structural changes have hard sync points. `CreateEntity`, `Instantiate`, `Destroy`, `AddComponent`, `RemoveComponent`, `SetSharedComponentData` all have a hard sync point. Meaning all jobs scheduled through `JobComponentSystem` will be completed before creating the `Entity`, for example. This happens automatically. So for instance: calling `EntityManager.CreateEntity` in the middle of the frame might result in a large stall waiting for all previously scheduled jobs in the `World` to complete.

See [EntityCommandBuffer](entity_command_buffer.md) for more on avoiding sync points when creating entities during game play.

## Multiple Worlds

Every `World` has its own `EntityManager` and thus a separate set of `JobHandle` dependency management. A hard sync point in one world will not affect the other `World`. As a result, for streaming and procedural generation scenarios, it is useful to create entities in one `World` and then move them to another `World` in one transaction at the beginning of the frame. 

See [ExclusiveEntityTransaction](exclusive_entity_transaction.md) for more on avoiding sync points for procedural generation & streaming scenarios and [System update order](system_update_order.md).



[Back to Unity Data-Oriented reference](reference.md)
