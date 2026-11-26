using Unity.Netcode;
using Unity.Entities;

using UnityEngine;

public struct AsteroidScore : IComponentData
{
    [GhostField] public int Value;
}
