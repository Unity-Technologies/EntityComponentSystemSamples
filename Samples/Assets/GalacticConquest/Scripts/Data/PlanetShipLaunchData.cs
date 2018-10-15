using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct PlanetShipLaunchData : IComponentData
    {
        public int NumberToSpawn;
        public float3 SpawnLocation;
        public float SpawnRadius;
        public Entity TargetEntity;
        public int TeamOwnership;
    }
}
