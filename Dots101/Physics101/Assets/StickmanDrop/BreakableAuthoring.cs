using Unity.Entities;
using UnityEngine;

namespace StickmanDrop
{
    public class BreakableAuthoring : MonoBehaviour
    {
        class Baker : Baker<BreakableAuthoring>
        {
            public override void Bake(BreakableAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Breakable>(entity);           
            }
        }
    }

    public class Breakable : IComponentData
    {
    }
}