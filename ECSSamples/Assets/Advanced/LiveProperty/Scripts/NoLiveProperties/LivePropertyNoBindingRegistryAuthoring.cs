using Unity.Entities;
using UnityEngine;

public class LivePropertyNoBindingRegistryAuthoring : MonoBehaviour
{
    public float FloatField = 10.0f;
    public int IntField = 5;
    public bool BoolField = true;

    class Baker : Baker<LivePropertyNoBindingRegistryAuthoring>
    {
        public override void Bake(LivePropertyNoBindingRegistryAuthoring authoring)
        {
            AddComponent(new LivePropertyBindingRegisteryComponent
                    {BindFloat = authoring.FloatField, BindInt = authoring.IntField, BindBool = authoring.BoolField});
        }
    }
}
