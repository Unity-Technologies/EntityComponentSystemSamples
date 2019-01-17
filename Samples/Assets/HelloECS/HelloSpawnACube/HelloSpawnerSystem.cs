using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// ComponentSystems run on the main thread. Use these when you have to do work that cannot be called from a job.
public class HelloSpawnerSystem : ComponentSystem
{
    ComponentGroup m_Spawners;

    protected override void OnCreateManager()
    {
        m_Spawners = GetComponentGroup(typeof(HelloSpawner), typeof(Position));
    }

    protected override void OnUpdate()
    {
        // Get all the spawners in the scene.
        using (var spawners = m_Spawners.ToEntityArray(Allocator.TempJob))
        {
            foreach (var spawner in spawners)
            {
                // Create an entity from the prefab set on the spawner component.
                var prefab = EntityManager.GetSharedComponentData<HelloSpawner>(spawner).prefab;
                var entity = EntityManager.Instantiate(prefab);

                // Copy the position of the spawner to the new entity.
                var position = EntityManager.GetComponentData<Position>(spawner);
                EntityManager.SetComponentData(entity, position);

                // Destroy the spawner so this system only runs once.
                EntityManager.DestroyEntity(spawner);
            }
        }
    }
}
