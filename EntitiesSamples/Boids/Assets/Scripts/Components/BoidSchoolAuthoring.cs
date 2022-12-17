#if UNITY_EDITOR

using Unity.Entities;
using UnityEngine;

namespace Samples.Boids
{
    public class BoidSchoolAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public float InitialRadius;
        public int Count;
        
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
    
    public struct BoidSchool : IComponentData
    {
        public Entity Prefab;
        public float InitialRadius;
        public int Count;
    }
}

#endif
