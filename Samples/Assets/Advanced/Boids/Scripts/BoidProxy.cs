using System;
using Unity.Entities;

[Serializable]
public struct Boid : ISharedComponentData
{
    public float cellRadius;
    public float separationWeight;
    public float alignmentWeight;
    public float targetWeight;
    public float obstacleAversionDistance;
}

public class BoidProxy : SharedComponentDataProxy<Boid> { }
