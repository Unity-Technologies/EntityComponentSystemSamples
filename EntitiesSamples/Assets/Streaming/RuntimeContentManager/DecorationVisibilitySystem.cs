using Unity.Burst;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Streaming.RuntimeContentManager
{
    // Creates jobs that compute visibility of the entities
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct DecorationVisibilitySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            new DecorationVisibilityJob
            {
                CamPos = Camera.main.transform.position,
                LoadRadius = Camera.main.farClipPlane,
                CamForward = Camera.main.transform.forward
            }.ScheduleParallel();
        }
    }

    // Job to compute the visibility of an entity and trigger loading and unloading
    [BurstCompile]
    partial struct DecorationVisibilityJob : IJobEntity
    {
        public float LoadRadius;
        public float3 CamPos;
        public float3 CamForward;

        void Execute(ref DecorationVisualComponentData dec, in LocalToWorld transform)
        {
            // "in view" just means within distance in this sample.
            var distToCamera = math.distance(transform.Position, CamPos);
            var newWithinLoadRange = distToCamera < LoadRadius;
            if (dec.withinLoadRange && !newWithinLoadRange)
            {
                dec.withinLoadRange = false;
                dec.shouldRender = false;
                dec.loaded = false;
                dec.mesh.Release();
                dec.material.Release();
            }
            else if (!dec.withinLoadRange && newWithinLoadRange)
            {
                dec.withinLoadRange = true;
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

                dec.shouldRender = distToCamera < LoadRadius * .25f ||
                                   math.distance(transform.Position, CamPos + CamForward * LoadRadius) < LoadRadius;
            }
        }
    }
}
