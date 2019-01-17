using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
// ISharedComponentData can have struct members with managed types.
public struct HelloSpawner : ISharedComponentData
{
    public GameObject prefab;
}

public class HelloSpawnerComponent : SharedComponentDataWrapper<HelloSpawner> { }
