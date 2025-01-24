using Unity.Entities;
using UnityEngine;

namespace GravityWell
{
    public class GravityWellAuthoring : MonoBehaviour
    {
        public float OrbitPos;  // orbit pos as angle in rads
        public class Baker : Baker<GravityWellAuthoring>
        {
            public override void Bake(GravityWellAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(entity, new GravityWell
                {
                    OrbitPos = authoring.OrbitPos
                });
            }
        }
    }
    
    public struct GravityWell : IComponentData
    {
        public float OrbitPos;   // in radians
    }
}

