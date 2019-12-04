using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

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
    internal override unsafe void OnBeforeInstantiatePrefab(BouncySpawnSettings spawnSettings)
    {
        var collider = EntityManager.GetComponentData<PhysicsCollider>(spawnSettings.Prefab);
        var material = ((ConvexColliderHeader*)collider.ColliderPtr)->Material;
        material.Restitution = spawnSettings.Restitution;
        ((ConvexColliderHeader*)collider.ColliderPtr)->Material = material;
    }
}
