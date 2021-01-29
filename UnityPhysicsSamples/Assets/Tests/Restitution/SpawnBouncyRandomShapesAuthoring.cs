using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.Assertions;
using Collider = Unity.Physics.Collider;

class SpawnBouncyRandomShapesAuthoring : SpawnRandomObjectsAuthoringBase<BouncySpawnSettings>
{
    public float restitution = 1f;

    internal override void Configure(ref BouncySpawnSettings spawnSettings) => spawnSettings.Restitution = restitution;
}

struct BouncySpawnSettings : IComponentData, ISpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }
    public float Restitution;
}

class SpawnBouncyRandomShapesSystem : SpawnRandomObjectsSystemBase<BouncySpawnSettings>
{
    private BlobAssetReference<Collider> TweakedCollider;

    internal override int GetRandomSeed(BouncySpawnSettings spawnSettings)
    {
        int seed = base.GetRandomSeed(spawnSettings);
        seed = (seed * 397) ^ (int)(spawnSettings.Restitution * 100);
        return seed;
    }

    internal override void OnBeforeInstantiatePrefab(ref BouncySpawnSettings spawnSettings)
    {
        base.OnBeforeInstantiatePrefab(ref spawnSettings);

        var component = EntityManager.GetComponentData<PhysicsCollider>(spawnSettings.Prefab);
        TweakedCollider = component.Value.Value.Clone();
        Assert.IsTrue(TweakedCollider.Value.CollisionType == CollisionType.Convex);
        unsafe
        {
            // TODO: remove when we have Material accessors
            unsafe
            {
                var header = (ConvexColliderHeader*)TweakedCollider.GetUnsafePtr();
                var material = header->Material;
                material.Restitution = spawnSettings.Restitution;
                header->Material = material;
            }
        }
    }

    internal override void ConfigureInstance(Entity instance, ref BouncySpawnSettings spawnSettings)
    {
        base.ConfigureInstance(instance, ref spawnSettings);
        var collider = EntityManager.GetComponentData<PhysicsCollider>(instance);
        collider.Value = TweakedCollider;
        EntityManager.SetComponentData(instance, collider);
    }
}
