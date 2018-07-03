using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(GravitySystem))]
public class AsteroidsMoveSystem : JobComponentSystem
{
    [BurstCompile]
    struct MoveJob : IJobParallelFor
    {
        public AsteroidData asteroids;
        public float dt;
        
        public void Execute(int index)
        {
            var position = asteroids.positions[index];
            position.Value += asteroids.velocities[index].Value * dt;
            asteroids.positions[index] = position;
        }
    }
    
    struct AsteroidData
    {
        public readonly int Length;
        [ReadOnly] public ComponentDataArray<Asteroid> asteroids;
        public ComponentDataArray<Position> positions;
        [ReadOnly] public ComponentDataArray<Velocity> velocities;
    }

    [Inject] private AsteroidData asteroidsData;
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var jobHandle = new MoveJob
        {
            asteroids = asteroidsData,
            dt = Time.deltaTime * SimulationBootstrap.settings.SimulationSpeed
        }.Schedule(asteroidsData.Length, 64, inputDeps);

        return jobHandle;
    }
}