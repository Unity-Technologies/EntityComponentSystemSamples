using Unity.Entities;
using UnityEngine;

namespace Blender
{
    public class BuoyancyAuthoring : MonoBehaviour
    {
        public float WaterLevel; // Height of the water surface (in world space)
        public float BuoyancyForce;
        public float Drag; // Water drag
        
        public class Baker : Baker<BuoyancyAuthoring>
        {
            public override void Bake(BuoyancyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Buoyancy
                {
                    WaterLevel = authoring.WaterLevel,
                    BuoyancyForce = authoring.BuoyancyForce,
                    Drag = authoring.Drag,
                });
                SetComponentEnabled<Buoyancy>(entity, false);
            }
        }
    }

    public struct Buoyancy : IComponentData, IEnableableComponent
    {
        public float WaterLevel;
        public float BuoyancyForce;
        public float Drag;
    }
}