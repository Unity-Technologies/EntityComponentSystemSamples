using Common.Scripts;
using Unity.Assertions;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Modify
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    partial class ExplosionSpawnSystem : PeriodicalySpawnRandomObjectsSystem<ExplosionSpawn>
    {
        internal override void ConfigureInstance(Entity instance, ref ExplosionSpawn spawnSettings)
        {
            Assert.IsTrue(EntityManager.HasComponent<ExplosionSpawnSettings>(instance));
            var explosionComponent = EntityManager.GetComponentData<ExplosionSpawnSettings>(instance);
            var localTransform = EntityManager.GetComponentData<LocalTransform>(instance);

            spawnSettings.Id--;

            // Setting the ID of a new explosion group
            // so that the group gets unique collider
            explosionComponent.Id = spawnSettings.Id;
            explosionComponent.Position = localTransform.Position;
            EntityManager.SetComponentData(instance, explosionComponent);
        }
    }
}
