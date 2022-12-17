#if UNITY_EDITOR

using System;
using Samples.Boids;
using Unity.Entities;
using UnityEngine;

namespace Samples.Boids
{
    public class BoidTargetAuthoring : MonoBehaviour
    {
        public class BoidTargetAuthoringBaker : Baker<BoidTargetAuthoring>
        {
            public override void Bake(BoidTargetAuthoring authoring)
            {
                AddComponent(new BoidTarget());
            }
        }
    }

    public struct BoidTarget : IComponentData
    {
    }
}

#endif