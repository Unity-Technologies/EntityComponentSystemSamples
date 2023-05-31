using Unity.Entities;
using UnityEngine;

namespace Streaming.BindingRegistry
{
    public class BoundAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(Example), nameof(Example.Float))]
        public float BoundFloat = 10.0f;

        [RegisterBinding(typeof(Example), nameof(Example.Int))]
        public int BoundInt = 5;

        [RegisterBinding(typeof(Example), nameof(Example.Bool))]
        public bool BoundBool = true;

        public float UnboundFloat;

        class Baker : Baker<BoundAuthoring>
        {
            public override void Bake(BoundAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Example
                {
                    Float = authoring.BoundFloat,
                    Int = authoring.BoundInt,
                    Bool = authoring.BoundBool,
                    UnboundFloat = authoring.UnboundFloat
                });
            }
        }
    }

    public struct Example : IComponentData
    {
        public float Float;
        public int Int;
        public bool Bool;
        public float UnboundFloat;
    }
}
