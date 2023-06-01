using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Events
{
    public class TriggerVolumePortalAuthoring : MonoBehaviour
    {
        public PhysicsBodyAuthoring CompanionPortal;

        class Baker : Baker<TriggerVolumePortalAuthoring>
        {
            public override void Bake(TriggerVolumePortalAuthoring authoring)
            {
                var companion = GetEntity(authoring.CompanionPortal, TransformUsageFlags.Dynamic);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TriggerVolumePortal
                {
                    Companion = companion,
                    TransferCount = 0
                });
            }
        }
    }

    public struct TriggerVolumePortal : IComponentData
    {
        public Entity Companion;

        // When an entity is teleported to its companion,
        // we increase the companion's TransferCount so that
        // the entity doesn't get immediately teleported
        // back to the original portal
        public int TransferCount;
    }
}
