using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

// This system updates all entities in the scene with both a RotationSpeed_ForEach and Rotation component.

// ReSharper disable once InconsistentNaming
public class RotationSpeedSystem_ForEach : JobComponentSystem
{
    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        float deltaTime = Time.DeltaTime;
        
        // Schedule job to rotate around up vector
        var jobHandle = Entities
            .WithName("RotationSpeedSystem_ForEach")
            .ForEach((ref Rotation rotation, in RotationSpeed_ForEach rotationSpeed) =>
             {
                 rotation.Value = math.mul(
                     math.normalize(rotation.Value), 
                     quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * deltaTime));
             })
            .Schedule(inputDependencies);
    
        
        // Return job handle as the dependency for this system
        return jobHandle;
    }
}
