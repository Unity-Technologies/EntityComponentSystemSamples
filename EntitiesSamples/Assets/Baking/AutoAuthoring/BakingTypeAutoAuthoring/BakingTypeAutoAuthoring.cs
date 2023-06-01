using System;
using AutoAuthoring;
using Unity.Entities;
using Unity.Mathematics;

namespace Baking.AutoAuthoring.BakingTypeAutoAuthoring
{
    // In this example, the component Complex is bundling together many values that make sense to be edited together.
    // By defining an AutoAuthoring<Complex> MonoBehaviour, the component is automatically visible and editable in the inspector.

    // The authoring component BakingTypeAutoAuthoring implements a custom Baker, allowing us to create a different representation for the runtime data.
    // For example, the baker adds a Speed component on the primary entity, ensuring an optimal data access for this property at runtime.
    // Furthermore, other properties are extracted from the authoring component in a baking system.

    // Because the Complex component has the attribute [BakingType], it will not be serialized in the final runtime data.

    // The same pattern can also be used to refactor the runtime data to ensure optimal runtime access while at the same time
    // giving the necessary flexibility to define the authoring components in a convenient way.
    // This facilitates easy prototyping while leaving open a path to optimize the data later.


    // Authoring component, optimized for convenient editing of the data.
    public class BakingTypeAutoAuthoring : AutoAuthoring<Complex>
    {
        class Baker : Baker<BakingTypeAutoAuthoring>
        {
            public override void Bake(BakingTypeAutoAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Speed { Value = authoring.Data.Properties.Speed });

                // We do not call GetEntity here because the Reference field is assigned in the ComponentAuthoring baker.
                AddComponent(entity, new SpawnPrefab() { Prefab = authoring.Data.Reference });
            }
        }
    }

    [Serializable]
    public struct Properties
    {
        public float3 Speed;
        public float3 Position;
        public float3 Rotation;
    }

    [BakingType]
    [Serializable]
    public struct Complex : IComponentData
    {
        public Entity Reference;
        public Properties Properties;
    }

    public struct Speed : IComponentData
    {
        public float3 Value;
    }

    public struct SpawnPrefab : IComponentData
    {
        public Entity Prefab;
    }
}
