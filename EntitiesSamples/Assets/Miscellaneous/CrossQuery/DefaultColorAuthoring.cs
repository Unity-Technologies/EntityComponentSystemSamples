using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Miscellaneous.CrossQuery
{
    public struct DefaultColor : IComponentData
    {
        public float4 Value;
    }

    public class DefaultColorAuthoring : MonoBehaviour
    {
        public Color WhenNotColliding;

        class Baker : Baker<DefaultColorAuthoring>
        {
            public override void Bake(DefaultColorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                DefaultColor component = default(DefaultColor);
                component.Value = (Vector4)authoring.WhenNotColliding;

                AddComponent(entity, component);
            }
        }
    }
}
