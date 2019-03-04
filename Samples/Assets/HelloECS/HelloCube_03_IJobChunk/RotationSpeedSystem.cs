using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.HelloCube_03
{
    // This system updates all entities in the scene with both a RotationSpeed and Rotation component.
    public class RotationSpeedSystem : JobComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            // Cached access to a set of ComponentData based on a specific query
            m_Group = GetComponentGroup(typeof(Rotation), ComponentType.ReadOnly<RotationSpeed>());
        }

        // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
        [BurstCompile]
        struct RotationSpeedJob : IJobChunk
        {
            public float DeltaTime;
            public ArchetypeChunkComponentType<Rotation> RotationType;
            [ReadOnly] public ArchetypeChunkComponentType<RotationSpeed> RotationSpeedType;
    
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkRotations = chunk.GetNativeArray(RotationType);
                var chunkRotationSpeeds = chunk.GetNativeArray(RotationSpeedType);
                for (var i = 0; i < chunk.Count; i++)
                {
                    var rotation = chunkRotations[i];
                    var rotationSpeed = chunkRotationSpeeds[i];
                    
                    // Rotate something about its up vector at the speed given by RotationSpeed.
                    chunkRotations[i] = new Rotation
                    {
                        Value = math.mul(math.normalize(rotation.Value),
                            quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime))
                    };
                }
            }
        }
    
        // OnUpdate runs on the main thread.
        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            // Explicitly declare:
            // - Read-Write access to Rotation
            // - Read-Only access to RotationSpeed
            var rotationType = GetArchetypeChunkComponentType<Rotation>(false); 
            var rotationSpeedType = GetArchetypeChunkComponentType<RotationSpeed>(true);
            
            var job = new RotationSpeedJob()
            {
                RotationType = rotationType,
                RotationSpeedType = rotationSpeedType,
                DeltaTime = Time.deltaTime
            };
    
            return job.Schedule(m_Group, inputDependencies);
        }
    }
}
