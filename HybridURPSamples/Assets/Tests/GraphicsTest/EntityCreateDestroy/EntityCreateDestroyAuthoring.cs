using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct EntityCreateDestroyTag : IComponentData
{
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class EntityCreateDestroyAuthoring: MonoBehaviour
    {
    }

    public class EntityCreateDestroyBaker : Baker<EntityCreateDestroyAuthoring>
    {
        public override void Bake(EntityCreateDestroyAuthoring authoring)
        {
            var data = default(EntityCreateDestroyTag);
            AddComponent(data);
        }
    }
}
