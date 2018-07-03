using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class GravitySystem : JobComponentSystem
{ 
    [BurstCompile]
    struct GravityJob : IJobParallelFor
    {
        public StarData stars;
        public AsteroidData asteroids;
        public float dt;
        public float G;
        
        public void Execute(int index)
        {
            var velocity = asteroids.velocities[index].Value;
            var mass = asteroids.masses[index].Value;
 
            for (int i = 0; i < stars.Length; i++)
            {
                var v = stars.positions[i].Value - asteroids.positions[index].Value;
                var f = G * ((stars.masses[i].Value * mass) / math.lengthSquared(v));
                velocity += (math.normalize(v) * f * dt) / mass;
            }
            
            asteroids.velocities[index] = new Velocity {Value = velocity};
        }
    }
    
    struct StarData
    {
        public readonly int Length;
        public GameObjectArray starsGO;
        [ReadOnly] public ComponentDataArray<Star> stars;
        [ReadOnly] public ComponentDataArray<Position> positions;
        [ReadOnly] public ComponentDataArray<Mass> masses;
    }

    struct AsteroidData
    {
        public readonly int Length;
        [ReadOnly] public ComponentDataArray<Asteroid> asteroids;
        [ReadOnly] public ComponentDataArray<Position> positions;
        [ReadOnly] public ComponentDataArray<Mass> masses;
        public ComponentDataArray<Velocity> velocities;
    }

    [Inject] private StarData starsData;
    [Inject] private AsteroidData asteroidsData;
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var jobHandle = new GravityJob
        {
            stars = starsData,
            asteroids = asteroidsData,
            dt = Time.deltaTime * SimulationBootstrap.settings.SimulationSpeed,
            G = SimulationBootstrap.settings.GravityConstant
        }.Schedule(asteroidsData.Length, 64, inputDeps);
        
        return jobHandle;
    }
}
