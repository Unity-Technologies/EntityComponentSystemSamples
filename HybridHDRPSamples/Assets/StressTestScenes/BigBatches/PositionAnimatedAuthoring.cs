using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct PositionAnimated : IComponentData
{
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class PositionAnimatedAuthoring : MonoBehaviour
    {
    }

    public class PositionAnimatedBaker : Baker<PositionAnimatedAuthoring>
    {
        public override void Bake(PositionAnimatedAuthoring authoring)
        {
            var data = default(PositionAnimated);
            AddComponent(data);
        }
    }
}
