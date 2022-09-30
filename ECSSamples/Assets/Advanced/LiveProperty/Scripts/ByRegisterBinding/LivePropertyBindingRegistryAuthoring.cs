using Unity.Entities;
using UnityEngine;

public struct LivePropertyBindingRegisteryComponent : IComponentData
{
    public float BindFloat;
    public int BindInt;
    public bool BindBool;
    public float NormalFloat;
}


public class LivePropertyBindingRegistryAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(LivePropertyBindingRegisteryComponent),
        nameof(LivePropertyBindingRegisteryComponent.BindFloat))]
    public float FloatField = 10.0f;

    [RegisterBinding(typeof(LivePropertyBindingRegisteryComponent),
        nameof(LivePropertyBindingRegisteryComponent.BindInt))]
    public int IntField = 5;

    [RegisterBinding(typeof(LivePropertyBindingRegisteryComponent),
        nameof(LivePropertyBindingRegisteryComponent.BindBool))]
    public bool BoolField = true;

    public float NormalFloatField;
}


public class LivePropertyBindingRegistryBaker : Baker<LivePropertyBindingRegistryAuthoring>
{
    public override void Bake(LivePropertyBindingRegistryAuthoring authoring)
    {
        AddComponent(new LivePropertyBindingRegisteryComponent
        {
            BindFloat = authoring.FloatField,
            BindInt = authoring.IntField,
            BindBool = authoring.BoolField,
            NormalFloat = authoring.NormalFloatField
        });
    }
}
