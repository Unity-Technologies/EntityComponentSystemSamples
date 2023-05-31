using Common.Scripts;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;

namespace Modify
{
    partial class ExplosionSpawnSettingsSystem : SpawnRandomObjectsSystemBase<ExplosionSpawnSettings>
    {
        // Used to divide colliders into groups, and to create a single collider for each group
        internal int GroupId;
        internal PhysicsCollider GroupCollider;
        internal NativeList<PhysicsCollider> CreatedColliders;

        protected override void OnCreate()
        {
            GroupId = 1;
            CreatedColliders = new NativeList<PhysicsCollider>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            foreach (var collider in CreatedColliders)
            {
                collider.Value.Dispose();
            }
            CreatedColliders.Dispose();
        }

        internal override void ConfigureInstance(Entity instance, ref ExplosionSpawnSettings explosionSpawnSettings)
        {
            // Create single collider per Explosion group
            if (GroupId != explosionSpawnSettings.Id)
            {
                GroupId = explosionSpawnSettings.Id;
                explosionSpawnSettings.Source = instance;

                var collider = EntityManager.GetComponentData<PhysicsCollider>(instance);
                var oldFilter = collider.Value.Value.GetCollisionFilter();

                // Only one of these needed per group, since all debris within
                // a group will share a single collider
                // This will make debris within a group collide some time after the explosion happens
                EntityManager.AddComponentData(instance, new FilterCountdown
                {
                    Countdown = explosionSpawnSettings.Countdown * 2,
                    Filter = oldFilter
                });

                // Make a unique collider for each spawned group
                BlobAssetReference<Collider> colliderCopy = collider.Value.Value.Clone();

                // Set the GroupIndex to GroupId, which is negative
                // This ensures that the debris within a group don't collide
                colliderCopy.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = oldFilter.BelongsTo,
                    CollidesWith = oldFilter.CollidesWith,
                    GroupIndex = GroupId,
                });

                PhysicsCollider newCollider = colliderCopy.AsComponent();
                GroupCollider = newCollider;
                CreatedColliders.Add(GroupCollider);
            }

            EntityManager.SetComponentData(instance, GroupCollider);

            EntityManager.AddComponentData(instance, new ExplosionCountdown
            {
                Source = explosionSpawnSettings.Source,
                Countdown = explosionSpawnSettings.Countdown,
                Center = explosionSpawnSettings.Position,
                Force = explosionSpawnSettings.Force
            });
        }
    }
}
