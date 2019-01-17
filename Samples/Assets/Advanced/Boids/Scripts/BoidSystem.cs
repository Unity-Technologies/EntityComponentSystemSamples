using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Samples.Common;

namespace Samples.Boids
{
    public class BoidSystem : JobComponentSystem
    {
        private ComponentGroup  m_BoidGroup;
        private ComponentGroup  m_TargetGroup;
        private ComponentGroup  m_ObstacleGroup;

        private List<Boid>      m_UniqueTypes = new List<Boid>(10);
        private List<PrevCells> m_PrevCells   = new List<PrevCells>();

        struct PrevCells
        {
            public NativeMultiHashMap<int, int> hashMap;
            public NativeArray<int>             cellIndices;
            public NativeArray<Position>        copyTargetPositions;
            public NativeArray<Position>        copyObstaclePositions;
            public NativeArray<Heading>         cellAlignment;
            public NativeArray<Position>        cellSeparation;
            public NativeArray<int>             cellObstaclePositionIndex;
            public NativeArray<float>           cellObstacleDistance;
            public NativeArray<int>             cellTargetPistionIndex;
            public NativeArray<int>             cellCount;
        }

        [BurstCompile]
        [RequireComponentTag(typeof(Boid))]
        struct HashPositions : IJobProcessComponentDataWithEntity<Position>
        {
            public NativeMultiHashMap<int, int>.Concurrent hashMap;
            public float                                   cellRadius;

            public void Execute(Entity entity, int index, [ReadOnly]ref Position position)
            {
                var hash = GridHash.Hash(position.Value, cellRadius);
                hashMap.Add(hash, index);            }
        }

        [BurstCompile]
        struct MergeCells : IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            public NativeArray<int>                 cellIndices;
            public NativeArray<Heading>             cellAlignment;
            public NativeArray<Position>            cellSeparation;
            public NativeArray<int>                 cellObstaclePositionIndex;
            public NativeArray<float>               cellObstacleDistance;
            public NativeArray<int>                 cellTargetPistionIndex;
            public NativeArray<int>                 cellCount;
            [ReadOnly] public NativeArray<Position> targetPositions;
            [ReadOnly] public NativeArray<Position> obstaclePositions;

            void NearestPosition(NativeArray<Position> targets, float3 position, out int nearestPositionIndex, out float nearestDistance )
            {
                nearestPositionIndex = 0;
                nearestDistance      = math.lengthsq(position-targets[0].Value);
                for (int i = 1; i < targets.Length; i++)
                {
                    var targetPosition = targets[i].Value;
                    var distance       = math.lengthsq(position-targetPosition);
                    var nearest        = distance < nearestDistance;

                    nearestDistance      = math.select(nearestDistance, distance, nearest);
                    nearestPositionIndex = math.select(nearestPositionIndex, i, nearest);
                }
                nearestDistance = math.sqrt(nearestDistance);
            }

            public void ExecuteFirst(int index)
            {
                var position = cellSeparation[index].Value / cellCount[index];

                int obstaclePositionIndex;
                float obstacleDistance;
                NearestPosition(obstaclePositions, position, out obstaclePositionIndex, out obstacleDistance);
                cellObstaclePositionIndex[index] = obstaclePositionIndex;
                cellObstacleDistance[index]      = obstacleDistance;

                int targetPositionIndex;
                float targetDistance;
                NearestPosition(targetPositions, position, out targetPositionIndex, out targetDistance);
                cellTargetPistionIndex[index] = targetPositionIndex;

                cellIndices[index] = index;
            }

            public void ExecuteNext(int cellIndex, int index)
            {
                cellCount[cellIndex]      += 1;
                cellAlignment[cellIndex]  = new Heading { Value = cellAlignment[cellIndex].Value + cellAlignment[index].Value };
                cellSeparation[cellIndex] = new Position { Value = cellSeparation[cellIndex].Value + cellSeparation[index].Value };
                cellIndices[index]        = cellIndex;
            }
        }

        [BurstCompile]
        [RequireComponentTag(typeof(Boid))]
        struct Steer : IJobProcessComponentDataWithEntity<Position, Heading>
        {
            [ReadOnly] public NativeArray<int>             cellIndices;
            [ReadOnly] public Boid                         settings;
            [ReadOnly] public NativeArray<Position>        targetPositions;
            [ReadOnly] public NativeArray<Position>        obstaclePositions;
            [ReadOnly] public NativeArray<Heading>         cellAlignment;
            [ReadOnly] public NativeArray<Position>        cellSeparation;
            [ReadOnly] public NativeArray<int>             cellObstaclePositionIndex;
            [ReadOnly] public NativeArray<float>           cellObstacleDistance;
            [ReadOnly] public NativeArray<int>             cellTargetPistionIndex;
            [ReadOnly] public NativeArray<int>             cellCount;
            public float                                   dt;

            public void Execute(Entity entity, int index, [ReadOnly]ref Position position, ref Heading heading)
            {
                var forward                           = heading.Value;
                var currentPosition                   = position.Value;
                var cellIndex                         = cellIndices[index];
                var neighborCount                     = cellCount[cellIndex];
                var alignment                         = cellAlignment[cellIndex].Value;
                var separation                        = cellSeparation[cellIndex].Value;
                var nearestObstacleDistance           = cellObstacleDistance[cellIndex];
                var nearestObstaclePositionIndex      = cellObstaclePositionIndex[cellIndex];
                var nearestTargetPositionIndex        = cellTargetPistionIndex[cellIndex];
                var nearestObstaclePosition           = obstaclePositions[nearestObstaclePositionIndex].Value;
                var nearestTargetPosition             = targetPositions[nearestTargetPositionIndex].Value;

                var obstacleSteering                  = currentPosition - nearestObstaclePosition;
                var avoidObstacleHeading              = (nearestObstaclePosition + math.normalizesafe(obstacleSteering)
                                                        * settings.obstacleAversionDistance)- currentPosition;
                var targetHeading                     = settings.targetWeight
                                                        * math.normalizesafe(nearestTargetPosition - currentPosition);
                var nearestObstacleDistanceFromRadius = nearestObstacleDistance - settings.obstacleAversionDistance;
                var alignmentResult                   = settings.alignmentWeight
                                                        * math.normalizesafe((alignment/neighborCount)-forward);
                var separationResult                  = settings.separationWeight
                                                        * math.normalizesafe((currentPosition * neighborCount) - separation);
                var normalHeading                     = math.normalizesafe(alignmentResult + separationResult + targetHeading);
                var targetForward                     = math.select(normalHeading, avoidObstacleHeading, nearestObstacleDistanceFromRadius < 0);
                var nextHeading                       = math.normalizesafe(forward + dt*(targetForward-forward));

                heading = new Heading {Value = nextHeading};
            }
        }

        protected override void OnStopRunning()
        {
            for (var i = 0; i < m_PrevCells.Count; i++)
            {
                m_PrevCells[i].hashMap.Dispose();
                m_PrevCells[i].cellIndices.Dispose();
                m_PrevCells[i].copyTargetPositions.Dispose();
                m_PrevCells[i].copyObstaclePositions.Dispose();
                m_PrevCells[i].cellAlignment.Dispose();
                m_PrevCells[i].cellSeparation.Dispose();
                m_PrevCells[i].cellObstacleDistance.Dispose();
                m_PrevCells[i].cellObstaclePositionIndex.Dispose();
                m_PrevCells[i].cellTargetPistionIndex.Dispose();
                m_PrevCells[i].cellCount.Dispose();
            }
            m_PrevCells.Clear();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityManager.GetAllUniqueSharedComponentData(m_UniqueTypes);

            var obstacleCount = m_ObstacleGroup.CalculateLength();
            var targetCount = m_TargetGroup.CalculateLength();

            // Ingore typeIndex 0, can't use the default for anything meaningful.
            for (int typeIndex = 1; typeIndex < m_UniqueTypes.Count; typeIndex++)
            {
                var settings = m_UniqueTypes[typeIndex];
                m_BoidGroup.SetFilter(settings);

                var boidCount  = m_BoidGroup.CalculateLength();

                var cacheIndex                = typeIndex - 1;
                var cellIndices               = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var hashMap                   = new NativeMultiHashMap<int,int>(boidCount,Allocator.TempJob);
                var cellObstacleDistance      = new NativeArray<float>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellObstaclePositionIndex = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellTargetPositionIndex   = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellCount                 = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);

                var cellAlignment             = m_BoidGroup.ToComponentDataArray<Heading>(Allocator.TempJob, out var initialCellAlignmentJobHandle);
                var cellSeparation            = m_BoidGroup.ToComponentDataArray<Position>(Allocator.TempJob, out var initialCellSeparationJobHandle);
                var copyTargetPositions       = m_TargetGroup.ToComponentDataArray<Position>(Allocator.TempJob, out var copyTargetPositionsJobHandle);
                var copyObstaclePositions     = m_ObstacleGroup.ToComponentDataArray<Position>(Allocator.TempJob, out var copyObstaclePositionsJobHandle);

                var nextCells = new PrevCells
                {
                    cellIndices               = cellIndices,
                    hashMap                   = hashMap,
                    copyObstaclePositions     = copyObstaclePositions,
                    copyTargetPositions       = copyTargetPositions,
                    cellAlignment             = cellAlignment,
                    cellSeparation            = cellSeparation,
                    cellObstacleDistance      = cellObstacleDistance,
                    cellObstaclePositionIndex = cellObstaclePositionIndex,
                    cellTargetPistionIndex    = cellTargetPositionIndex,
                    cellCount                 = cellCount
                };

                if (cacheIndex > (m_PrevCells.Count - 1))
                {
                    m_PrevCells.Add(nextCells);
                }
                else
                {
                    m_PrevCells[cacheIndex].hashMap.Dispose();
                    m_PrevCells[cacheIndex].cellIndices.Dispose();
                    m_PrevCells[cacheIndex].cellObstaclePositionIndex.Dispose();
                    m_PrevCells[cacheIndex].cellTargetPistionIndex.Dispose();
                    m_PrevCells[cacheIndex].copyTargetPositions.Dispose();
                    m_PrevCells[cacheIndex].copyObstaclePositions.Dispose();
                    m_PrevCells[cacheIndex].cellAlignment.Dispose();
                    m_PrevCells[cacheIndex].cellSeparation.Dispose();
                    m_PrevCells[cacheIndex].cellObstacleDistance.Dispose();
                    m_PrevCells[cacheIndex].cellCount.Dispose();
                }
                m_PrevCells[cacheIndex] = nextCells;

                var hashPositionsJob = new HashPositions
                {
                    hashMap        = hashMap.ToConcurrent(),
                    cellRadius     = settings.cellRadius
                };
                var hashPositionsJobHandle = hashPositionsJob.ScheduleGroup(m_BoidGroup, inputDeps);

                var initialCellCountJob = new MemsetNativeArray<int>
                {
                    Source = cellCount,
                    Value  = 1
                };
                var initialCellCountJobHandle = initialCellCountJob.Schedule(boidCount, 64, inputDeps);

                var initialCellBarrierJobHandle = JobHandle.CombineDependencies(initialCellAlignmentJobHandle, initialCellSeparationJobHandle, initialCellCountJobHandle);
                var copyTargetObstacleBarrierJobHandle = JobHandle.CombineDependencies(copyTargetPositionsJobHandle, copyObstaclePositionsJobHandle);
                var mergeCellsBarrierJobHandle = JobHandle.CombineDependencies(hashPositionsJobHandle, initialCellBarrierJobHandle, copyTargetObstacleBarrierJobHandle);

                var mergeCellsJob = new MergeCells
                {
                    cellIndices               = cellIndices,
                    cellAlignment             = cellAlignment,
                    cellSeparation            = cellSeparation,
                    cellObstacleDistance      = cellObstacleDistance,
                    cellObstaclePositionIndex = cellObstaclePositionIndex,
                    cellTargetPistionIndex    = cellTargetPositionIndex,
                    cellCount                 = cellCount,
                    targetPositions           = copyTargetPositions,
                    obstaclePositions         = copyObstaclePositions
                };
                var mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap,64,mergeCellsBarrierJobHandle);

                var steerJob = new Steer
                {
                    cellIndices               = nextCells.cellIndices,
                    settings                  = settings,
                    cellAlignment             = cellAlignment,
                    cellSeparation            = cellSeparation,
                    cellObstacleDistance      = cellObstacleDistance,
                    cellObstaclePositionIndex = cellObstaclePositionIndex,
                    cellTargetPistionIndex    = cellTargetPositionIndex,
                    cellCount                 = cellCount,
                    targetPositions           = copyTargetPositions,
                    obstaclePositions         = copyObstaclePositions,
                    dt                        = Time.deltaTime,
                };
                var steerJobHandle = steerJob.ScheduleGroup(m_BoidGroup, mergeCellsJobHandle);

                inputDeps = steerJobHandle;
                m_BoidGroup.AddDependency(inputDeps);
            }
            m_UniqueTypes.Clear();

            return inputDeps;
        }

        protected override void OnCreateManager()
        {
            m_BoidGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(Boid)),
                ComponentType.ReadOnly(typeof(Position)),
                typeof(Heading));
            m_TargetGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(BoidTarget)),
                ComponentType.ReadOnly(typeof(Position)));
            m_ObstacleGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(BoidObstacle)),
                ComponentType.ReadOnly(typeof(Position)));
        }
    }
}