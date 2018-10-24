using Data;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

namespace Systems
{
    /// <summary>
    /// Increases the numbers of ships that can be sent from a planet
    /// </summary>
    [UpdateAfter(typeof(ShipSpawnSystem))]
    public class OccupantIncreaseSystem : JobComponentSystem
    {
        float spawnCounter = 0.0f;
        float spawnInterval = 0.1f;
        int occupantsToSpawn = 100;

        struct Planets
        {
#pragma warning disable 649
            public readonly int Length;

            public ComponentDataArray<PlanetData> Data;
#pragma warning restore 649        
        }

        struct PlanetsOccupantsJob : IJobParallelFor
        {
            public ComponentDataArray<PlanetData> Data;
            [ReadOnly]
            public int OccupantsToSpawn;

            public void Execute(int index)
            {
                var data = Data[index];
                if (data.TeamOwnership == 0)
                    return;
                data.Occupants += OccupantsToSpawn;
                Data[index] = data;
            }
        }

        [Inject]
#pragma warning disable 649
        Planets planets;
#pragma warning restore 649

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var deltaTime = Time.deltaTime;
            spawnCounter += deltaTime;
            if (spawnCounter < spawnInterval)
                return inputDeps;
            spawnCounter = 0.0f;

            var job = new PlanetsOccupantsJob
            {
                Data = planets.Data,
                OccupantsToSpawn = occupantsToSpawn
            };

            return job.Schedule(planets.Length, 32, inputDeps);
        }
    }
}
