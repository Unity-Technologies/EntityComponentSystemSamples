using Unity.Burst;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Streaming.RuntimeContentManager
{
    //Creates jobs that compute visibility of the entities
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct DecorationVisibilitySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            new DecorationVisibilityJob
            {
                camPos = Camera.main.transform.position,
                loadRadius = Camera.main.farClipPlane,
                camFwd = Camera.main.transform.forward
            }.ScheduleParallel();
        }
    }

    // Job to compute the visibility of an entity and trigger loading and unloading
    [BurstCompile]
    partial struct DecorationVisibilityJob : IJobEntity
    {
        public float loadRadius;
        public float3 camPos;
        public float3 camFwd;

        void Execute(ref DecorationVisualComponentData dec, in LocalToWorld transform)
        {
            // "in view" just means within distance in this sample.
            var distToCamera = math.distance(transform.Position, camPos);
            var newWithinLoadRange = distToCamera < loadRadius;
            if (dec.withinLoadRange && !newWithinLoadRange)
            {
                dec.shouldRender = false;
                dec.loaded = false;
                dec.mesh.Release();
                dec.material.Release();
            }
            else if (!dec.withinLoadRange && newWithinLoadRange)
            {
                dec.mesh.LoadAsync();
                dec.material.LoadAsync();
            }

            dec.withinLoadRange = newWithinLoadRange;
            if (newWithinLoadRange)
            {
                if (!dec.loaded)
                {
                    dec.loaded = dec.material.LoadingStatus >= ObjectLoadingStatus.Completed &&
                                 dec.mesh.LoadingStatus >= ObjectLoadingStatus.Completed;
                }

                dec.shouldRender = distToCamera < loadRadius * .25f ||
                                   math.distance(transform.Position, camPos + camFwd * loadRadius) < loadRadius;
            }
        }
    }
}
