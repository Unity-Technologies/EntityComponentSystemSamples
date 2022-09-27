using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct RotationAndScale : IComponentData
{
    public quaternion R;
    public float3 S;

    public void ComputeTransform(float time)
    {
        float3 p = new float3(1, 0, 0);
        float s = math.sin(time);
        float c = math.cos(time);
        float2x2 m = new float2x2(c, -s, s, c);
        p.xy = math.mul(m, p.xy);
        p.xy = math.mul(m, p.xy);
        p.yz = math.mul(m, p.yz);
        p.zx = math.mul(m, p.zx);
        p.zx = math.mul(m, p.zx);
        p.zx = math.mul(m, p.zx);

        float minScale = 1.4f;
        float uniformScale = math.pow(s, 63) * math.sin(time + 1.5f) * 8 + minScale;

        float xScale = (math.sin(1.5f * time) + 0.9f) * 0.5f;
        float yScale = (math.sin(1.7f * time) + 0.9f) * 0.5f;
        float zScale = (math.sin(1.9f * time) + 0.9f) * 0.5f;

        R = quaternion.LookRotationSafe(p, new float3(0, 1, 0));
        S = new float3(uniformScale) * new float3(xScale, yScale, zScale);
    }
}

[DisallowMultipleComponent]
public class RotationAndScaleAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(RotationAndScale), "R")]
    public quaternion R;
    [RegisterBinding(typeof(RotationAndScale), "S.x", true)]
    [RegisterBinding(typeof(RotationAndScale), "S.y", true)]
    [RegisterBinding(typeof(RotationAndScale), "S.z", true)]
    public float3 S;

    class RotationAndScaleBaker : Baker<RotationAndScaleAuthoring>
    {
        public override void Bake(RotationAndScaleAuthoring authoring)
        {
            RotationAndScale component = default(RotationAndScale);
            component.R = authoring.R;
            component.S = authoring.S;
            AddComponent(component);
        }
    }
}

[RequireMatchingQueriesForUpdate]
public partial class RotationAndScaleSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float t = (float)SystemAPI.Time.ElapsedTime;

        Entities.ForEach((ref RotationAndScale rs, ref LocalToWorld localToWorld, in Translation translation) =>
        {
            rs.ComputeTransform(t);
            localToWorld.Value = float4x4.TRS(translation.Value, rs.R, rs.S);
        }).Run();
    }
}
