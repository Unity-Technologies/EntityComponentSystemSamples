using Unity.Entities;
using UnityEngine;

namespace Unity.DotsUISample
{
    public class CauldronAuthoring : MonoBehaviour
    {
        public class Baker : Baker<CauldronAuthoring>
        {
            public override void Bake(CauldronAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Cauldron>(entity);
            }
        }
    }
    
    public struct Cauldron : IComponentData
    {
    }
}