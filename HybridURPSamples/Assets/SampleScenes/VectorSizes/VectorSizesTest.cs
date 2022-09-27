using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[ExecuteInEditMode]
[ExecuteAlways]
public class VectorSizesTest : MonoBehaviour
{
    public Color color;
    [Range(1, 4)]
    public int channels = 4;

    // Start is called before the first frame update
    void Start()
    {
        UpdatePropertyBlock();
    }

    public void OnValidate()
    {
        UpdatePropertyBlock();
    }

    // In GameObject mode, use slow MaterialPropertyBlock path
    // to output a reference for comparison with Entities
    private void UpdatePropertyBlock()
    {
        float4 c = new float4(color.r, color.g, color.b, 1);
        var block = new MaterialPropertyBlock();
        float f1 = 0;
        float4 f2 = default;
        float4 f3 = default;
        float4 f4 = default;
        switch (channels)
        {
            case 1:
                f1 = c.x;
                break;
            case 2:
                f2 = c;
                break;
            case 3:
                f3 = c;
                break;
            case 4:
            default:
                f4 = c;
                break;
        }
        block.SetFloat("_F1", f1);
        block.SetVector("_F2", f2);
        block.SetVector("_F3", f3);
        block.SetVector("_F4", f4);

        GetComponent<MeshRenderer>().SetPropertyBlock(block);
    }
}

// Tightly packed test components for each vector size. Use of float3 is not recommended in practice
// due to poor GPU performance.
[MaterialProperty("_F1")] public struct TestF1 : IComponentData { public float  Value; }
[MaterialProperty("_F2")] public struct TestF2 : IComponentData { public float2 Value; }
[MaterialProperty("_F3")] public struct TestF3 : IComponentData { public float3 Value; }
[MaterialProperty("_F4")] public struct TestF4 : IComponentData { public float4 Value; }

public class VectorSizesTestBaker : Baker<VectorSizesTest>
{
    public override void Bake(VectorSizesTest vst)
    {
        var color = vst.color;
        float4 c = new float4(color.r, color.g, color.b, 1);

        switch (vst.channels)
        {
            case 1:
                AddComponent(new TestF1 { Value = c.x });
                break;
            case 2:
                AddComponent(new TestF2 { Value = c.xy });
                break;
            case 3:
                AddComponent(new TestF3 { Value = c.xyz });
                break;
            case 4:
            default:
                AddComponent(new TestF4 { Value = c });
                break;
        }
    }
}
