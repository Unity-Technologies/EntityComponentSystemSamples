using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

// This system updates all entities in the scene with both a HelloMovementSpeed and Position component.
public class HelloMovementSpeedSystem : JobComponentSystem
{
    // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
    [BurstCompile]
    struct HelloMovementSpeedJob : IJobProcessComponentData<Position, HelloMovementSpeed>
    {
        // The [ReadOnly] attribute tells the job scheduler that this job cannot write to dT.
        [ReadOnly] public float dT;

        // Position is read/write
        public void Execute(ref Position Position, [ReadOnly] ref HelloMovementSpeed movementSpeed)
        {
            // Move something in the +X direction at the speed given by HelloMovementSpeed.
            // If this thing's X position is more than 2x its speed, reset X position to 0.
            float moveSpeed = movementSpeed.Value * dT;
            float moveLimit = movementSpeed.Value * 2;
            Position.Value.x = Position.Value.x < moveLimit ? Position.Value.x + moveSpeed : 0 ;
        }
    }

    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new HelloMovementSpeedJob()
        {
            dT = Time.deltaTime
        };

        return job.Schedule(this, inputDependencies);
    }
}
