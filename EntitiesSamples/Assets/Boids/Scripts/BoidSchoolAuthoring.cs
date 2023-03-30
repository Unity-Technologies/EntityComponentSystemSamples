using Unity.Entities;
using UnityEngine;

namespace Boids
{
    public class BoidSchoolAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public float InitialRadius;
        public int Count;

        class Baker : Baker<BoidSchoolAuthoring>
        {
            public override void Bake(BoidSchoolAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new BoidSchool
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
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
