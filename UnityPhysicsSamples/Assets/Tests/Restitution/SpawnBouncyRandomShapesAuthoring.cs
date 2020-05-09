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

    internal override void OnBeforeInstantiatePrefab(BouncySpawnSettings spawnSettings)
    {
        base.OnBeforeInstantiatePrefab(spawnSettings);
        var component = EntityManager.GetComponentData<PhysicsCollider>(spawnSettings.Prefab);
        unsafe
        {
            var oldCollider = component.ColliderPtr;
            var newCollider = (Collider*)UnsafeUtility.Malloc(oldCollider->MemorySize, 16, Allocator.Temp);
            
            UnsafeUtility.MemCpy(newCollider, oldCollider, oldCollider->MemorySize);

            var material = ((ConvexColliderHeader*)newCollider)->Material;
            material.Restitution = spawnSettings.Restitution;
            ((ConvexColliderHeader*)newCollider)->Material = material;

            Assert.IsTrue(oldCollider->MemorySize == newCollider->MemorySize, "Error when cloning Collider!");

            TweakedCollider = BlobAssetReference<Collider>.Create(newCollider, newCollider->MemorySize);

            UnsafeUtility.Free(newCollider, Allocator.Temp);
        }
    }

    internal override void ConfigureInstance(Entity instance, BouncySpawnSettings spawnSettings)
    {
        base.ConfigureInstance(instance, spawnSettings);
        var collider = EntityManager.GetComponentData<PhysicsCollider>(instance);
        collider.Value = TweakedCollider;
        EntityManager.SetComponentData(instance, collider);
    }
}

