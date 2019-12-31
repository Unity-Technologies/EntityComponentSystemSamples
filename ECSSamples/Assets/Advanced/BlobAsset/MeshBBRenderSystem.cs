using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[ExecuteAlways]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public class MeshBBRenderSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref MeshBBComponent dmc, ref Translation t) =>
        {
            var min = dmc.BlobData.Value.MinBoundingBox;
            var max = dmc.BlobData.Value.MaxBoundingBox;
      
            // Draw a bounding box
            Debug.DrawLine(t.Value + new float3(min.x, min.y, min.z), t.Value + new float3(max.x, min.y, min.z));
            Debug.DrawLine(t.Value + new float3(min.x, max.y, min.z), t.Value + new float3(max.x, max.y, min.z));
            Debug.DrawLine(t.Value + new float3(min.x, min.y, min.z), t.Value + new float3(min.x, max.y, min.z));
            Debug.DrawLine(t.Value + new float3(max.x, min.y, min.z), t.Value + new float3(max.x, max.y, min.z));
        
            Debug.DrawLine(t.Value + new float3(min.x, min.y, max.z), t.Value + new float3(max.x, min.y, max.z));
            Debug.DrawLine(t.Value + new float3(min.x, max.y, max.z), t.Value + new float3(max.x, max.y, max.z));
            Debug.DrawLine(t.Value + new float3(min.x, min.y, max.z), t.Value + new float3(min.x, max.y, max.z));
            Debug.DrawLine(t.Value + new float3(max.x, min.y, max.z), t.Value + new float3(max.x, max.y, max.z));
        
            Debug.DrawLine(t.Value + new float3(min.x, min.y, min.z), t.Value + new float3(min.x, min.y, max.z));
            Debug.DrawLine(t.Value + new float3(max.x, min.y, min.z), t.Value + new float3(max.x, min.y, max.z));
            Debug.DrawLine(t.Value + new float3(min.x, max.y, min.z), t.Value + new float3(min.x, max.y, max.z));
            Debug.DrawLine(t.Value + new float3(max.x, max.y, min.z), t.Value + new float3(max.x, max.y, max.z));
        });
    }
}