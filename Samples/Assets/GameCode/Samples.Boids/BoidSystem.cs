using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Mathematics.Experimental;
using Unity.Transforms;
using UnityEngine;
using Samples.Common;

namespace Samples.Boids
{
    [UpdateBefore(typeof(TransformInputBarrier))]
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
        struct HashPositions : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<Position> positions;
            public NativeMultiHashMap<int, int>.Concurrent hashMap;
            public float                                   cellRadius;

            public void Execute(int index)
            {
                var hash = GridHash.Hash(positions[index].Value, cellRadius);
                hashMap.Add(hash, index);
            }
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
                nearestDistance      = math.lengthSquared(position-targets[0].Value);
                for (int i = 1; i < targets.Length; i++)
                {
                    var targetPosition = targets[i].Value;
                    var distance       = math.lengthSquared(position-targetPosition);
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
        struct Steer : IJobParallelFor
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
            [ReadOnly] public ComponentDataArray<Position> positions;
            public float                                   dt;
            public ComponentDataArray<Heading>             headings;
            
            public void Execute(int index)
            {
                var forward                           = headings[index].Value;
                var position                          = positions[index].Value;
                var cellIndex                         = cellIndices[index];
                var neighborCount                     = cellCount[cellIndex];
                var alignment                         = cellAlignment[cellIndex].Value;
                var separation                        = cellSeparation[cellIndex].Value;
                var nearestObstacleDistance           = cellObstacleDistance[cellIndex];
                var nearestObstaclePositionIndex      = cellObstaclePositionIndex[cellIndex];
                var nearestTargetPositionIndex        = cellTargetPistionIndex[cellIndex];
                var nearestObstaclePosition           = obstaclePositions[nearestObstaclePositionIndex].Value;
                var nearestTargetPosition             = targetPositions[nearestTargetPositionIndex].Value;
                
                var obstacleSteering                  = position - nearestObstaclePosition;
                var avoidObstacleHeading              = (nearestObstaclePosition + math_experimental.normalizeSafe(obstacleSteering)
                                                        * settings.obstacleAversionDistance)-position;
                var targetHeading                     = settings.targetWeight 
                                                        * math_experimental.normalizeSafe(nearestTargetPosition - position);
                var nearestObstacleDistanceFromRadius = nearestObstacleDistance - settings.obstacleAversionDistance;
                var alignmentResult                   = settings.alignmentWeight 
                                                        * math_experimental.normalizeSafe((alignment/neighborCount)-forward);
                var separationResult                  = settings.separationWeight 
                                                        * math_experimental.normalizeSafe((position * neighborCount) - separation);
                var normalHeading                     = math_experimental.normalizeSafe(alignmentResult + separationResult + targetHeading);
                var targetForward                     = math.select(normalHeading, avoidObstacleHeading, nearestObstacleDistanceFromRadius < 0);
                var nextHeading                       = math_experimental.normalizeSafe(forward + dt*(targetForward-forward));
                
                headings[index]                       = new Heading {Value = nextHeading};
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
            EntityManager.GetAllUniqueSharedComponentDatas(m_UniqueTypes);
            
            var obstaclePositions = m_ObstacleGroup.GetComponentDataArray<Position>();
            var targetPositions   = m_TargetGroup.GetComponentDataArray<Position>();
            
            // Ingore typeIndex 0, can't use the default for anything meaningful.
            for (int typeIndex = 1; typeIndex < m_UniqueTypes.Count; typeIndex++)
            {
                var settings = m_UniqueTypes[typeIndex];
                m_BoidGroup.SetFilter(settings);
                
                var positions                 = m_BoidGroup.GetComponentDataArray<Position>();
                var headings                  = m_BoidGroup.GetComponentDataArray<Heading>();
                var cacheIndex                = typeIndex - 1;
                var boidCount                 = positions.Length;
                var cellIndices               = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var hashMap                   = new NativeMultiHashMap<int,int>(boidCount,Allocator.TempJob);
                var copyTargetPositions       = new NativeArray<Position>(targetPositions.Length, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var copyObstaclePositions     = new NativeArray<Position>(obstaclePositions.Length, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellAlignment             = new NativeArray<Heading>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellSeparation            = new NativeArray<Position>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellObstacleDistance      = new NativeArray<float>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellObstaclePositionIndex = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellTargetPositionIndex   = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
                var cellCount                 = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);

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
                    positions      = positions,
                    hashMap        = hashMap,
                    cellRadius     = settings.cellRadius
                };
                var hashPositionsJobHandle = hashPositionsJob.Schedule(boidCount, 64, inputDeps);

                var initialCellAlignmentJob = new CopyComponentData<Heading>
                {
                    Source  = headings,
                    Results = cellAlignment
                };
                var initialCellAlignmentJobHandle = initialCellAlignmentJob.Schedule(boidCount, 64, inputDeps);
                
                var initialCellSeparationJob = new CopyComponentData<Position>
                {
                    Source  = positions,
                    Results = cellSeparation
                };
                var initialCellSeparationJobHandle = initialCellSeparationJob.Schedule(boidCount, 64, inputDeps);
                
                var initialCellCountJob = new MemsetNativeArray<int>
                {
                    Source = cellCount,
                    Value  = 1
                };
                var initialCellCountJobHandle = initialCellCountJob.Schedule(boidCount, 64, inputDeps);

                var initialCellBarrierJobHandle = JobHandle.CombineDependencies(initialCellAlignmentJobHandle, initialCellSeparationJobHandle, initialCellCountJobHandle);

                var copyTargetPositionsJob = new CopyComponentData<Position>
                {
                    Source  = targetPositions,
                    Results = copyTargetPositions
                };
                var copyTargetPositionsJobHandle = copyTargetPositionsJob.Schedule(targetPositions.Length, 2, inputDeps);
                
                var copyObstaclePositionsJob = new CopyComponentData<Position>
                {
                    Source  = obstaclePositions,
                    Results = copyObstaclePositions
                };
                var copyObstaclePositionsJobHandle = copyObstaclePositionsJob.Schedule(obstaclePositions.Length, 2, inputDeps);

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
                    positions                 = positions,
                    headings                  = headings,
                };
                var steerJobHandle = steerJob.Schedule(boidCount, 64, mergeCellsJobHandle);
                    
                inputDeps = steerJobHandle;
            }
            m_UniqueTypes.Clear();
            
            return inputDeps;
        }

        protected override void OnCreateManager(int capacity)
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
