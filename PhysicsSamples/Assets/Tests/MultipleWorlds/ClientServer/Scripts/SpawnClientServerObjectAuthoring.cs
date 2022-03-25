using System;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using System.Collections.Generic;
using Unity.Transforms;

class SpawnClientServerObjectAuthoring : SpawnRandomObjectsAuthoringBase<ClientServerObjectSpawnSettings>
{
    public DriveGhostBodyAuthoring ClientPrefab;

    internal override void Configure(ref ClientServerObjectSpawnSettings spawnSettings, Entity serverEntity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        spawnSettings.ClientPrefab = conversionSystem.GetPrimaryEntity(ClientPrefab);
    }

    internal override void Configure(List<GameObject> referencedPrefabs)
    {
        base.Configure(referencedPrefabs);

        referencedPrefabs.Add(ClientPrefab.gameObject);
    }
}

struct ClientServerObjectSpawnSettings : IComponentData, ISpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }
    public Entity ClientPrefab { get; set; }
}

class SpawnClientServerObjectSystem : SpawnRandomObjectsSystemBase<ClientServerObjectSpawnSettings>
{
    internal override int GetRandomSeed(ClientServerObjectSpawnSettings spawnSettings)
    {
        var seed = base.GetRandomSeed(spawnSettings);
        seed = (seed * 397) ^ spawnSettings.ClientPrefab.GetHashCode();
        return seed;
    }

    internal override void ConfigureInstance(Entity serverInstance, ref ClientServerObjectSpawnSettings spawnSettings)
    {
        // Create a ghost instance for the client world
        var clientInstance = EntityManager.Instantiate(spawnSettings.ClientPrefab);

        // Set the ghost to be driven from the server instance
        var driveGhost = EntityManager.GetComponentData<DriveGhostBodyData>(clientInstance);
        driveGhost.DrivingEntity = serverInstance;
        EntityManager.SetComponentData(clientInstance, driveGhost);

        // Set the starting transform of the ghost on the client to match the server instance
        EntityManager.SetComponentData(clientInstance, EntityManager.GetComponentData<Translation>(serverInstance));
        EntityManager.SetComponentData(clientInstance, EntityManager.GetComponentData<Rotation>(serverInstance));
    }
}
