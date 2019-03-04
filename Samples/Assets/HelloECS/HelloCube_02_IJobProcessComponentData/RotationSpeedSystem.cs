using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.HelloCube_02
{
    // This system updates all entities in the scene with both a RotationSpeed and Rotation component.
    public class RotationSpeedSystem : JobComponentSystem
    {
        // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
        [BurstCompile]
        struct RotationSpeedJob : IJobProcessComponentData<Rotation, RotationSpeed>
        {
            public float DeltaTime;
    
            // The [ReadOnly] attribute tells the job scheduler that this job will not write to rotSpeed
            public void Execute(ref Rotation rotation, [ReadOnly] ref RotationSpeed rotSpeed)
            {
                // Rotate something about its up vector at the speed given by RotationSpeed.
                rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), rotSpeed.RadiansPerSecond * DeltaTime));
            }
        }
    
        // OnUpdate runs on the main thread.
        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var job = new RotationSpeedJob()
            {
                DeltaTime = Time.deltaTime
            };
    
            return job.Schedule(this, inputDependencies);
        }
    }
}
