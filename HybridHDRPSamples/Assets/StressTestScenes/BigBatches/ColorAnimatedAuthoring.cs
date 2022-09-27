using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct ColorAnimated : IComponentData
{
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class ColorAnimatedAuthoring : MonoBehaviour
    {
    }

    public class ColorAnimatedBaker : Baker<ColorAnimatedAuthoring>
    {
        public override void Bake(ColorAnimatedAuthoring authoring)
        {
            var data = default(ColorAnimated);
            AddComponent(data);
        }
    }
}