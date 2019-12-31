using Unity.Entities;

// ReSharper disable once InconsistentNaming
public class LifetimeSystem_HybridComponent : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref Lifetime_HybridComponent lifetime) =>
        {
            lifetime.timeRemainingInSeconds -= Time.DeltaTime;
            if(lifetime.timeRemainingInSeconds < 0)
                EntityManager.DestroyEntity(entity);
        });
    }
}
