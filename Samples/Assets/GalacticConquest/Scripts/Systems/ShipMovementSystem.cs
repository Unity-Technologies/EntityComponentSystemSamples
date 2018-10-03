using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

namespace Systems
{
    /// <summary>
    /// Moves all spawned ships towards their target planet
    /// If a ship enter their target planet it adds a ShipArrivedTag component to it for later processing
    /// If a ship enters the wrong planet it gets pushed out to the surface
    /// </summary>
    [UpdateAfter(typeof(ShipArrivalSystem))]
    public class ShipMovementSystem : JobComponentSystem
    {
#pragma warning disable 649
        struct Ships
        {

            public readonly int Length;

            public ComponentDataArray<Position> Positions;
            public ComponentDataArray<Rotation> Rotations;
            public ComponentDataArray<ShipData> Data;
            public EntityArray Entities;
        }

        struct Planets
        {
            public readonly int Length;
            public ComponentDataArray<PlanetData> Data;
        }
#pragma warning restore 649
        [BurstCompile]
        struct CalculatePositionsJob : IJobParallelFor
        {
            public float DeltaTime;
            [ReadOnly]
            public ComponentDataArray<ShipData> Ships;
            [ReadOnly] public EntityArray Entities;
            public ComponentDataArray<Position> Positions;
            public ComponentDataArray<Rotation> Rotations;

            [ReadOnly] public ComponentDataArray<PlanetData> Planets;
            [ReadOnly] public ComponentDataFromEntity<PlanetData> TargetPlanet;

            public NativeQueue<Entity>.Concurrent ShipArrivedQueue;

            public void Execute(int index)
            {
                var shipData = Ships[index];

                var targetPosition = TargetPlanet[shipData.TargetEntity].Position;
                var position = Positions[index];
                var rotation = Rotations[index];


                var newPos = Vector3.MoveTowards(position.Value, targetPosition, DeltaTime * 4.0f);

                for (var planetIndex = 0; planetIndex < Planets.Length; planetIndex++)
                {
                    var planet = Planets[planetIndex];
                    if (Vector3.Distance(newPos, planet.Position) < planet.Radius)
                    {
                        if (planet.Position == targetPosition)
                        {
                            ShipArrivedQueue.Enqueue(Entities[index]);
                        }
                        var direction = (newPos - planet.Position).normalized;
                        newPos = planet.Position + (direction * planet.Radius);
                        break;
                    }
                }

                var shipCurrentDirection = math.normalize((float3)newPos - position.Value);
                rotation.Value = quaternion.LookRotation(shipCurrentDirection, math.up());

                position.Value = newPos;
                Positions[index] = position;
                Rotations[index] = rotation;
            }
        }

        struct ShipArrivedTagJob : IJob
        {
            public EntityCommandBuffer EntityCommandBuffer;
            public NativeQueue<Entity> ShipArrivedQueue;

            public void Execute()
            {
                Entity entity;
                while (ShipArrivedQueue.TryDequeue(out entity))
                {
                    EntityCommandBuffer.AddComponent(entity, new ShipArrivedTag());
                }
            }
        }

#pragma warning disable 649
        [Inject]
        EndFrameBarrier m_EndFrameBarrier;

        [Inject]
        Ships m_Ships;
        [Inject]
        Planets m_Planets;
#pragma warning restore 649

        NativeQueue<Entity> m_ShipArrivedQueue;

        protected override void OnCreateManager()
        {
            m_ShipArrivedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            m_ShipArrivedQueue.Dispose();
            base.OnDestroyManager();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_Ships.Length == 0)
                return inputDeps;

            var handle = new CalculatePositionsJob
            {
                Ships = m_Ships.Data,
                Planets = m_Planets.Data,
                TargetPlanet = GetComponentDataFromEntity<PlanetData>(),
                DeltaTime = Time.deltaTime,
                Entities = m_Ships.Entities,
                Positions = m_Ships.Positions,
                Rotations = m_Ships.Rotations,
                ShipArrivedQueue = m_ShipArrivedQueue.ToConcurrent()
            }.Schedule(m_Ships.Length, 32, inputDeps);

            handle = new ShipArrivedTagJob
            {
                EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
                ShipArrivedQueue = m_ShipArrivedQueue
            }.Schedule(handle);

            return handle;
        }
    }
}
