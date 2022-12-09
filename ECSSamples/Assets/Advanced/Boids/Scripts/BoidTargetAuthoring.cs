#if UNITY_EDITOR

using System;
using Samples.Boids;
using Unity.Entities;
using UnityEngine;

[AddComponentMenu("DOTS Samples/Boids/BoidTarget")]
public class BoidTargetAuthoring : MonoBehaviour {}

public class BoidTargetAuthoringBaker : Baker<BoidTargetAuthoring>
{
    public override void Bake(BoidTargetAuthoring authoring)
    {
        AddComponent( new BoidTarget());
    }
}

#endif
