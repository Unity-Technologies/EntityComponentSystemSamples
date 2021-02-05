using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;

// This system updates all entities in the scene with both a RotationSpeed_IJobChunkStructBased and Rotation component.

// ReSharper disable once InconsistentNaming
[BurstCompile]
public struct RotationSpeedSystem_IJobChunkStructBased : ISystemBase
{
    EntityQuery m_Group;

    public void OnCreate(ref SystemState state)
    {
        // Cached access to a set of ComponentData based on a specific query
        m_Group = state.GetEntityQuery(typeof(Rotation), ComponentType.ReadOnly<RotationSpeed_IJobChunkStructBased>());
    }

    // Use the [BurstCompile] attribute to compile a job with Burst. You may see significant speed ups, so try it!
    [BurstCompile]
    struct RotationSpeedJob : IJobChunk
    {
        public float DeltaTime;
        public ComponentTypeHandle<Rotation> RotationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<RotationSpeed_IJobChunkStructBased> RotationSpeedTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkRotations = chunk.GetNativeArray(RotationTypeHandle);
            var chunkRotationSpeeds = chunk.GetNativeArray(RotationSpeedTypeHandle);
            for (var i = 0; i < chunk.Count; i++)
            {
                var rotation = chunkRotations[i];
                var rotationSpeed = chunkRotationSpeeds[i];

                // Rotate something about its up vector at the speed given by RotationSpeed_IJobChunkStructBased.
                chunkRotations[i] = new Rotation
                {
                    Value = math.mul(math.normalize(rotation.Value),
                        quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime))
                };
            }
        }
    }

    // OnUpdate runs on the main thread.
    // Note that from 2020.2 the update function itself can be burst compiled when using struct systems.
#if UNITY_2020_2_OR_NEWER
    [BurstCompile]
#endif
    public void OnUpdate(ref SystemState state)
    {
        // Explicitly declare:
        // - Read-Write access to Rotation
        // - Read-Only access to RotationSpeed_IJobChunkStructBased
        var rotationType = state.GetComponentTypeHandle<Rotation>();
        var rotationSpeedType = state.GetComponentTypeHandle<RotationSpeed_IJobChunkStructBased>(true);

        var job = new RotationSpeedJob()
        {
            RotationTypeHandle = rotationType,
            RotationSpeedTypeHandle = rotationSpeedType,
            DeltaTime = state.Time.DeltaTime
        };

        state.Dependency = job.ScheduleSingle(m_Group, state.Dependency);
    }

    public void OnDestroy(ref SystemState state)
    {
    }
}
