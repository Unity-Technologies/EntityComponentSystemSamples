#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Samples.Boids
{
    [AddComponentMenu("DOTS Samples/Boids/BoidSchool")]
    public class BoidSchoolAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public float InitialRadius;
        public int Count;
    }

    public class BoidSchoolAuthoringBaker : Baker<BoidSchoolAuthoring>
    {
        public override void Bake(BoidSchoolAuthoring authoring)
        {
            AddComponent( new BoidSchool
            {
                Prefab = GetEntity(authoring.Prefab),
                Count = authoring.Count,
                InitialRadius = authoring.InitialRadius
            });
        }
    }
}

#endif
