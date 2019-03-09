using System;
using Unity.Entities;
using Unity.Transforms;

[Serializable]
[WriteGroup(typeof(LocalToWorld))]
public struct Boid : ISharedComponentData
{
    public float CellRadius;
    public float SeparationWeight;
    public float AlignmentWeight;
    public float TargetWeight;
    public float ObstacleAversionDistance;
    public float MoveSpeed;
}

public class BoidProxy : SharedComponentDataProxy<Boid> { }
