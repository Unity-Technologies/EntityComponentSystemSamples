using Unity.Entities;
using Unity.Mathematics;

public struct LivePropertyComponent : IComponentData
{
    public float FloatField;
    public int IntField;
    public bool BoolField;

    public float2 Float2Field;
    public int2 Int2Field;
    public bool2 Bool2Field;

    public float3 Float3Field;
    public int3 Int3Field;
    public bool3 Bool3Field;

    public float4 Float4Field;
    public int4 Int4Field;
    public bool4 Bool4Field;
}

[UnityEngine.DisallowMultipleComponent]
public class LivePropertyComponentAuthoring : UnityEngine.MonoBehaviour
{
    [RegisterBinding(typeof(LivePropertyComponent), "FloatField")]
    public float FloatField;
    [RegisterBinding(typeof(LivePropertyComponent), "IntField")]
    public int IntField;
    [RegisterBinding(typeof(LivePropertyComponent), "BoolField")]
    public bool BoolField;
    [RegisterBinding(typeof(LivePropertyComponent), "Float2Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Float2Field.y", true)]
    public float2 Float2Field;
    [RegisterBinding(typeof(LivePropertyComponent), "Int2Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Int2Field.y", true)]
    public int2 Int2Field;
    [RegisterBinding(typeof(LivePropertyComponent), "Bool2Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Bool2Field.y", true)]
    public bool2 Bool2Field;
    [RegisterBinding(typeof(LivePropertyComponent), "Float3Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Float3Field.y", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Float3Field.z", true)]
    public float3 Float3Field;
    [RegisterBinding(typeof(LivePropertyComponent), "Int3Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Int3Field.y", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Int3Field.z", true)]
    public int3 Int3Field;
    [RegisterBinding(typeof(LivePropertyComponent), "Bool3Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Bool3Field.y", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Bool3Field.z", true)]
    public bool3 Bool3Field;
    [RegisterBinding(typeof(LivePropertyComponent), "Float4Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Float4Field.y", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Float4Field.z", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Float4Field.w", true)]
    public float4 Float4Field;
    [RegisterBinding(typeof(LivePropertyComponent), "Int4Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Int4Field.y", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Int4Field.z", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Int4Field.w", true)]
    public int4 Int4Field;
    [RegisterBinding(typeof(LivePropertyComponent), "Bool4Field.x", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Bool4Field.y", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Bool4Field.z", true)]
    [RegisterBinding(typeof(LivePropertyComponent), "Bool4Field.w", true)]
    public bool4 Bool4Field;

    class LivePropertyComponentBaker : Baker<LivePropertyComponentAuthoring>
    {
        public override void Bake(LivePropertyComponentAuthoring authoring)
        {
            LivePropertyComponent component = default(LivePropertyComponent);
            component.FloatField = authoring.FloatField;
            component.IntField = authoring.IntField;
            component.BoolField = authoring.BoolField;
            component.Float2Field = authoring.Float2Field;
            component.Int2Field = authoring.Int2Field;
            component.Bool2Field = authoring.Bool2Field;
            component.Float3Field = authoring.Float3Field;
            component.Int3Field = authoring.Int3Field;
            component.Bool3Field = authoring.Bool3Field;
            component.Float4Field = authoring.Float4Field;
            component.Int4Field = authoring.Int4Field;
            component.Bool4Field = authoring.Bool4Field;
            AddComponent(component);
        }
    }
}
