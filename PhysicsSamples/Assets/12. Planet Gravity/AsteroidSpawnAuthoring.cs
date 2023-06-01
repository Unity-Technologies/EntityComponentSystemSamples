using System;
using Common.Scripts;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PlanetGravity
{
    class AsteroidSpawnAuthoring : SpawnRandomObjectsAuthoringBase<AsteroidSpawn>
    {
        public float massFactor = 1;

        internal override void Configure(ref AsteroidSpawn spawnSettings) =>
            spawnSettings.MassFactor = massFactor;

        class Baker : SpawnRandomObjectsAuthoringBaseBaker<AsteroidSpawnAuthoring, AsteroidSpawn>
        {
            internal override void Configure(AsteroidSpawnAuthoring authoring,
                ref AsteroidSpawn spawnSettings) => spawnSettings.MassFactor = authoring.massFactor;
        }
    }

    public struct AsteroidSpawn : IComponentData, ISpawnSettings
    {
        public Entity Prefab { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public float3 Range { get; set; }
        public int Count { get; set; }
        public int RandomSeedOffset { get; set; }
        public float MassFactor;
    }
}
