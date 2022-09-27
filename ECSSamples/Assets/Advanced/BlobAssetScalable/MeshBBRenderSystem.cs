using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
public partial class MeshBBRenderSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<MeshBBComponent>();
    }

    protected override void OnUpdate()
    {
#if !ENABLE_TRANSFORM_V1
        Entities.ForEach((Entity e, ref MeshBBComponent dmc, ref LocalToWorldTransform t) =>
#else
        Entities.ForEach((Entity e, ref MeshBBComponent dmc, ref Translation t) =>
#endif
        {
            var min = dmc.BlobData.Value.MinBoundingBox;
            var max = dmc.BlobData.Value.MaxBoundingBox;

#if !ENABLE_TRANSFORM_V1
            var pos = t.Value.Position;
#else
            var pos = t.Value;
#endif
            // Draw a bounding box
            Debug.DrawLine(pos + new float3(min.x, min.y, min.z), pos + new float3(max.x, min.y, min.z));
            Debug.DrawLine(pos + new float3(min.x, max.y, min.z), pos + new float3(max.x, max.y, min.z));
            Debug.DrawLine(pos + new float3(min.x, min.y, min.z), pos + new float3(min.x, max.y, min.z));
            Debug.DrawLine(pos + new float3(max.x, min.y, min.z), pos + new float3(max.x, max.y, min.z));

            Debug.DrawLine(pos + new float3(min.x, min.y, max.z), pos + new float3(max.x, min.y, max.z));
            Debug.DrawLine(pos + new float3(min.x, max.y, max.z), pos + new float3(max.x, max.y, max.z));
            Debug.DrawLine(pos + new float3(min.x, min.y, max.z), pos + new float3(min.x, max.y, max.z));
            Debug.DrawLine(pos + new float3(max.x, min.y, max.z), pos + new float3(max.x, max.y, max.z));

            Debug.DrawLine(pos + new float3(min.x, min.y, min.z), pos + new float3(min.x, min.y, max.z));
            Debug.DrawLine(pos + new float3(max.x, min.y, min.z), pos + new float3(max.x, min.y, max.z));
            Debug.DrawLine(pos + new float3(min.x, max.y, min.z), pos + new float3(min.x, max.y, max.z));
            Debug.DrawLine(pos + new float3(max.x, max.y, min.z), pos + new float3(max.x, max.y, max.z));
        }).ScheduleParallel();
    }
}
