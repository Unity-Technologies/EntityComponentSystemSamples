using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

// This system updates all entities in the scene with both a RotationSpeed_SpawnAndRemove and Rotation component.

// ReSharper disable once InconsistentNaming
public class RotationSpeedSystem_SpawnAndRemove : JobComponentSystem
{
    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var deltaTime = Time.DeltaTime;
        
        // The in keyword on the RotationSpeed_SpawnAndRemove component tells the job scheduler that this job will not write to rotSpeedSpawnAndRemove
        return Entities
            .WithName("RotationSpeedSystem_SpawnAndRemove")
            .ForEach((ref Rotation rotation, in RotationSpeed_SpawnAndRemove rotSpeedSpawnAndRemove) =>
        {
            // Rotate something about its up vector at the speed given by RotationSpeed_SpawnAndRemove.
            rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), rotSpeedSpawnAndRemove.RadiansPerSecond * deltaTime));
        }).Schedule(inputDependencies);
    }
}
