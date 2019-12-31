#if UNITY_EDITOR

using Samples.Boids;
using System;
using Unity.Entities;
using UnityEngine;

[AddComponentMenu("DOTS Samples/Boids/Boid")]
public class BoidAuthoring : MonoBehaviour
{
    public float CellRadius = 8.0f;
    public float SeparationWeight = 1.0f;
    public float AlignmentWeight = 1.0f;
    public float TargetWeight = 2.0f;
    public float ObstacleAversionDistance = 30.0f;
    public float MoveSpeed = 25.0f;
}

#endif
