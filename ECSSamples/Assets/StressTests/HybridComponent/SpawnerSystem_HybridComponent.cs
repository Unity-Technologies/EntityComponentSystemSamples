using Unity.Entities;

// ReSharper disable once InconsistentNaming
public class SpawnerSystem_HybridComponent : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Spawner_HybridComponent spawner) =>
        {
            spawner.timeToNextSpawnInSeconds -= Time.DeltaTime;
            if (spawner.timeToNextSpawnInSeconds < 0)
            {
                EntityManager.Instantiate(spawner.prefab);
                spawner.timeToNextSpawnInSeconds = 1;
            }
        });
    }
}
