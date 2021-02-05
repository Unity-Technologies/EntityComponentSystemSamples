using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

// This system updates all entities in the scene with both a RotationSpeed_IJobChunk and Rotation component.

// ReSharper disable once InconsistentNaming
public partial class RotationSpeedSystem_IJobChunk : SystemBase
{
    EntityQuery m_Query;

    protected override void OnCreate()
    {
        // Cached access to a set of ComponentData based on a specific query
        m_Query = GetEntityQuery(typeof(Rotation), ComponentType.ReadOnly<RotationSpeed_IJobEntityBatch>());
    }

    // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
    [BurstCompile]
    struct RotationSpeedJob : IJobEntityBatch
    {
        public float DeltaTime;
        public ComponentTypeHandle<Rotation> RotationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<RotationSpeed_IJobEntityBatch> RotationSpeedTypeHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
            var chunkRotationSpeeds = batchInChunk.GetNativeArray(RotationSpeedTypeHandle);
            for (var i = 0; i < batchInChunk.Count; i++)
            {
                var rotation = chunkRotations[i];
                var rotationSpeed = chunkRotationSpeeds[i];

                // Rotate something about its up vector at the speed given by RotationSpeed_IJobChunk.
                chunkRotations[i] = new Rotation
                {
                    Value = math.mul(math.normalize(rotation.Value),
                        quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime))
                };
            }
        }
    }

    // OnUpdate runs on the main thread.
    protected override void OnUpdate()
    {
        // Explicitly declare:
        // - Read-Write access to Rotation
        // - Read-Only access to RotationSpeed_IJobChunk
        var rotationType = GetComponentTypeHandle<Rotation>();
        var rotationSpeedType = GetComponentTypeHandle<RotationSpeed_IJobEntityBatch>(true);

        var job = new RotationSpeedJob()
        {
            RotationTypeHandle = rotationType,
            RotationSpeedTypeHandle = rotationSpeedType,
            DeltaTime = Time.DeltaTime
        };

        Dependency = job.ScheduleParallel(m_Query, 1, Dependency);
    }
}
