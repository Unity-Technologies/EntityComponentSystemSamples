using Unity.Entities;
using UnityEngine;

namespace Unity.DotsUISample
{
    public class EnergyAuthoring : MonoBehaviour
    {
        public int Index;
        
        public class Baker : Baker<EnergyAuthoring>
        {
            public override void Bake(EnergyAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(entity, new Energy
                {
                    Index = authoring.Index
                });
            }
        }
    }
    
    public struct Energy : IComponentData
    {
        public bool Collected;
        public int Index;  // index of the energy collectable
    }
}