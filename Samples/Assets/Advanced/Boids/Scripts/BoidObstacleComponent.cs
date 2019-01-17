using System;
using Unity.Entities;

[Serializable]
public struct BoidObstacle : IComponentData { }

[UnityEngine.DisallowMultipleComponent]
public class BoidObstacleComponent : ComponentDataWrapper<BoidObstacle> { }
