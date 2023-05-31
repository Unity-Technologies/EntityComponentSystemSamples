using Unity.Entities;
using UnityEngine;

namespace Streaming.BindingRegistry
{
    public class UnboundAuthoring : MonoBehaviour
    {
        public float Float = 10.0f;
        public int Int = 5;
        public bool Bool = true;

        class Baker : Baker<UnboundAuthoring>
        {
            public override void Bake(UnboundAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Example
                {
                    Float = authoring.Float,
                    Int = authoring.Int,
                    Bool = authoring.Bool
                });
            }
        }
    }
}
