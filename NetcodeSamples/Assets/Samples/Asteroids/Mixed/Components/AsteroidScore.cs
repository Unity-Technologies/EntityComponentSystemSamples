using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct AsteroidScore : IComponentData
{
    [GhostField] public int Value;
}
