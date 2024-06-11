using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using UnityEngine.Assertions;
using Collider = Unity.Physics.Collider;

class SpawnBouncyRandomShapesAuthoring : SpawnRandomObjectsAuthoringBase<BouncySpawnSettings>
{
    public float restitution = 1f;

    internal override void Configure(ref BouncySpawnSettings spawnSettings) => spawnSettings.Restitution = restitution;
}

class SpawnBouncyRandomShapesAuthoringBaker : SpawnRandomObjectsAuthoringBaseBaker<SpawnBouncyRandomShapesAuthoring, BouncySpawnSettings>
{
    internal override void Configure(SpawnBouncyRandomShapesAuthoring authoring,
        ref BouncySpawnSettings spawnSettings)
    {
        spawnSettings.Restitution = authoring.restitution;
    }
}

struct BouncySpawnSettings : IComponentData, ISpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }
    public int RandomSeedOffset { get; set; }
    public float Restitution;
}

partial class SpawnBouncyRandomShapesSystem : SpawnRandomObjectsSystemBase<BouncySpawnSettings>
{
    private NativeList<BlobAssetReference<Collider>> m_CollidersToDispose;
    private BlobAssetReference<Collider> m_CurrentTweakedCollider;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_CollidersToDispose = new NativeList<BlobAssetReference<Collider>>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        foreach (var collider in m_CollidersToDispose)
            if (collider.IsCreated)
                collider.Dispose();
        m_CollidersToDispose.Dispose();
        // CurrentTweakedCollider is already in the list to dispose, and does not need to be disposed separately.
        base.OnDestroy();
    }

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
        m_CurrentTweakedCollider = component.Value.Value.Clone();
        Assert.IsTrue(m_CurrentTweakedCollider.Value.CollisionType == CollisionType.Convex);
        m_CollidersToDispose.Add(m_CurrentTweakedCollider);

        unsafe
        {
            ref var cc = ref m_CurrentTweakedCollider.As<ConvexCollider>();
            var material = cc.Material;
            material.Restitution = spawnSettings.Restitution;
            cc.Material = material;
        }
    }

    internal override void ConfigureInstance(Entity instance, ref BouncySpawnSettings spawnSettings)
    {
        base.ConfigureInstance(instance, ref spawnSettings);
        var collider = EntityManager.GetComponentData<PhysicsCollider>(instance);
        collider.Value = m_CurrentTweakedCollider;
        EntityManager.SetComponentData(instance, collider);
    }
}
