using Unity.Entities;
using UnityEngine;

namespace Tutorials.Firefighters
{
    public class PondAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PondAuthoring>
        {
            public override void Bake(PondAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Pond>(entity);
            }
        }
    }

    public struct Pond : IComponentData
    {

    }
}

