using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// This system updates all entities in the scene with both a RotationSpeed_SpawnAndRemove and Rotation component.

// ReSharper disable once InconsistentNaming
public class RotationSpeedSystem_SpawnAndRemove : JobComponentSystem
{
    // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
    [BurstCompile]
    struct RotationSpeedJob : IJobForEach<Rotation, RotationSpeed_SpawnAndRemove>
    {
        public float DeltaTime;

        // The [ReadOnly] attribute tells the job scheduler that this job will not write to rotSpeedSpawnAndRemove
        public void Execute(ref Rotation rotation, [ReadOnly] ref RotationSpeed_SpawnAndRemove rotSpeedSpawnAndRemove)
        {
            // Rotate something about its up vector at the speed given by RotationSpeed_SpawnAndRemove.
            rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), rotSpeedSpawnAndRemove.RadiansPerSecond * DeltaTime));
        }
    }

    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new RotationSpeedJob
        {
            DeltaTime = Time.deltaTime
        };

        return job.Schedule(this, inputDependencies);
    }
}
