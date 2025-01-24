using Unity.Entities;
using UnityEngine;

namespace Blender
{
    public class BuoyancyZoneAuthoring : MonoBehaviour
    {
        public float WaterLevel; // Height of the water surface (in world space)
        public float BuoyancyForce;
        public float Drag; // Water drag

        public class Baker : Baker<BuoyancyZoneAuthoring>
        {
            public override void Bake(BuoyancyZoneAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BuoyancyZone
                {
                    Buoyancy = new Buoyancy
                    {
                        WaterLevel = authoring.WaterLevel,
                        BuoyancyForce = authoring.BuoyancyForce,
                        Drag = authoring.Drag,    
                    }
                });
            }
        }
    }

    public struct BuoyancyZone : IComponentData
    {
        public Buoyancy Buoyancy;
    }
}