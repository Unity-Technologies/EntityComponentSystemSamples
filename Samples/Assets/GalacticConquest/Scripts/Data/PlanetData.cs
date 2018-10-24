using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Data
{
    public struct PlanetData : IComponentData
    {
        public int TeamOwnership;
        public int Occupants;
        public Vector3 Position;
        public float Radius;
    }

    public struct RotationData : IComponentData
    {
        public float3 RotationSpeed;
    }

    public struct ShipArrivedTag : IComponentData
    {}
}
