﻿using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

class PeriodicallySpawnRandomShapesAuthoring : SpawnRandomObjectsAuthoringBase<PeriodicSpawnSettings>
{
    public int SpawnRate = 50;
    public int DeathRate = 50;

    internal override void Configure(ref PeriodicSpawnSettings spawnSettings)
    {
        spawnSettings.SpawnRate = SpawnRate;
        spawnSettings.DeathRate = DeathRate;
    }
}

struct PeriodicSpawnSettings : IComponentData, ISpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }

    // Every SpawnRate frames, Count new Prefabs will spawned. 
    public int SpawnRate;
    // Spawned Prefabs will be removed after DeathRate frames. 
    public int DeathRate;
}

class PeriodicallySpawnRandomShapeSystem : SpawnRandomObjectsSystemBase<PeriodicSpawnSettings>
{
    public int FrameCount = 0;

    internal override int GetRandomSeed(PeriodicSpawnSettings spawnSettings)
    {
        int seed = base.GetRandomSeed(spawnSettings);
        seed = (seed * 397) ^ spawnSettings.Prefab.GetHashCode();
        seed = (seed * 397) ^ spawnSettings.DeathRate;
        seed = (seed * 397) ^ spawnSettings.SpawnRate;
        seed = (seed * 397) ^ FrameCount;
        return seed;
    }


    internal override unsafe void OnBeforeInstantiatePrefab(PeriodicSpawnSettings spawnSettings)
    {
        if( !EntityManager.HasComponent<EntityKiller>(spawnSettings.Prefab) )
        {
            EntityManager.AddComponent<EntityKiller>(spawnSettings.Prefab);
        }
        EntityManager.SetComponentData(spawnSettings.Prefab, new EntityKiller { TimeToDie = spawnSettings.DeathRate });
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref PeriodicSpawnSettings spawnSettings) =>
        {
            if (FrameCount % spawnSettings.SpawnRate == 0)
            {
                var count = spawnSettings.Count;

                OnBeforeInstantiatePrefab(spawnSettings);

                var instances = new NativeArray<Entity>(count, Allocator.Temp);
                EntityManager.Instantiate(spawnSettings.Prefab, instances);

                var positions = new NativeArray<float3>(count, Allocator.Temp);
                var rotations = new NativeArray<quaternion>(count, Allocator.Temp);
                RandomPointsOnCircle(spawnSettings.Position, spawnSettings.Range, ref positions, ref rotations, GetRandomSeed(spawnSettings));

                for (int i = 0; i < count; i++)
                {
                    var instance = instances[i];
                    EntityManager.SetComponentData(instance, new Translation { Value = positions[i] });
                    EntityManager.SetComponentData(instance, new Rotation { Value = rotations[i] });
                    ConfigureInstance(instance, spawnSettings);
                }
            }
        });
        FrameCount++;
    }
}
