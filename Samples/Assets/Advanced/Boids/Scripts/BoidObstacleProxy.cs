using System;
using Unity.Entities;

public struct BoidObstacle : IComponentData { }

[UnityEngine.DisallowMultipleComponent]
public class BoidObstacleProxy : ComponentDataProxy<BoidObstacle> { }
