using System;
using Unity.Entities;
using UnityEngine;

namespace Modify
{
    [Serializable]
    public class ModificationTypeAuthoring : MonoBehaviour
    {
        public ModificationType.Type ModificationType;

        class Baker : Baker<ModificationTypeAuthoring>
        {
            public override void Bake(ModificationTypeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ModificationType
                {
                    type = authoring.ModificationType
                });
            }
        }
    }

    public struct ModificationType : IComponentData
    {
        public enum Type
        {
            None,
            SoftContact,
            SurfaceVelocity,
            InfiniteInertia,
            BiggerInertia,
            NoAngularEffects,
            DisabledContact,
            DisabledAngularFriction,
        }

        public Type type;
    }
}
