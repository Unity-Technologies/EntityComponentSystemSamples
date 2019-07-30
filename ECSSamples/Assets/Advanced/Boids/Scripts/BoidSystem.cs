using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Mike's GDC Talk on 'A Data Oriented Approach to Using Component Systems'
// is a great reference for disecting the Boids sample code:
// https://youtu.be/p65Yt20pw0g?t=1446
// It explains a slightly older implementation of this sample but almost all the
// information is still relevant.

// The targets (2 red fish) and obstacle (1 shark) move based on the ActorAnimation tab
// in the Unity UI, so that they are moving based on key-framed animation.

namespace Samples.Boids
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class BoidSystem : JobComponentSystem
    {
        private EntityQuery  m_BoidQuery;
        private EntityQuery  m_TargetQuery;
        private EntityQuery  m_ObstacleQuery;

        // In this sample there are 3 total unique boid variants, one for each unique value of the 
        // Boid SharedComponent (note: this includes the default uninitialized value at
        // index 0, which isnt actually used in the sample).
        private List<Boid>                               m_UniqueTypes = new List<Boid>(3);
        private List<NativeMultiHashMap<int, int>> m_PrevFrameHashmaps = new List<NativeMultiHashMap<int, int>>();

        // `CopyPositions` and `CopyHeadings` are both for extracting the relevant position, heading component
        // to NativeArrays so that they can be randomly accessed by the `MergeCells` and `Steer` jobs
        
        [BurstCompile]
        struct CopyPositions : IJobForEachWithEntity<LocalToWorld>
        {
            public NativeArray<float3> positions;

            public void Execute(Entity entity, int index, [ReadOnly]ref LocalToWorld localToWorld)
            {
                positions[index] = localToWorld.Position;
            }
        }

        [BurstCompile]
        struct CopyHeadings : IJobForEachWithEntity<LocalToWorld>
        {
            public NativeArray<float3> headings;

            public void Execute(Entity entity, int index, [ReadOnly]ref LocalToWorld localToWorld)
            {
                headings[index] = localToWorld.Forward;
            }
        }
        
        // Populates a hash map, where each bucket contains the indices of all Boids whose positions quantize
        // to the same value for a given cell radius so that the information can be randomly accessed by
        // the `MergeCells` and `Steer` jobs.
        [BurstCompile]
        [RequireComponentTag(typeof(Boid))]
        struct HashPositions : IJobForEachWithEntity<LocalToWorld>
        {
            public NativeMultiHashMap<int, int>.ParallelWriter hashMap;
            public float                                       cellRadius;

            public void Execute(Entity entity, int index, [ReadOnly]ref LocalToWorld localToWorld)
            {
                var hash = (int)math.hash(new int3(math.floor(localToWorld.Position / cellRadius)));
                hashMap.Add(hash, index);
            }
        }
        
        // This accumulates the `positions` (separations) and `headings` (alignments) of all the Boids in each cell
        // in order to do the following:
        // 1) count the number of Boids in each cell
        // 2) find the nearest obstacle and target to each boid cell
        // 3) track which array entry contains the accumulated values for each Boid's cell
        [BurstCompile]
        struct MergeCells : IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            public NativeArray<int>                 cellIndices;
            public NativeArray<float3>              cellAlignment;
            public NativeArray<float3>              cellSeparation;
            public NativeArray<int>                 cellObstaclePositionIndex;
            public NativeArray<float>               cellObstacleDistance;
            public NativeArray<int>                 cellTargetPositionIndex;
            public NativeArray<int>                 cellCount;
            [ReadOnly] public NativeArray<float3>   targetPositions;
            [ReadOnly] public NativeArray<float3>   obstaclePositions;

            void NearestPosition(NativeArray<float3> targets, float3 position, out int nearestPositionIndex, out float nearestDistance )
            {
                nearestPositionIndex = 0;
                nearestDistance      = math.lengthsq(position-targets[0]);
                for (int i = 1; i < targets.Length; i++)
                {
                    var targetPosition = targets[i];
                    var distance       = math.lengthsq(position-targetPosition);
                    var nearest        = distance < nearestDistance;

                    nearestDistance      = math.select(nearestDistance, distance, nearest);
                    nearestPositionIndex = math.select(nearestPositionIndex, i, nearest);
                }
                nearestDistance = math.sqrt(nearestDistance);
            }
            
            // Resolves the distance of the nearest obstacle and target and stores the cell index.
            public void ExecuteFirst(int index)
            {
                var position = cellSeparation[index] / cellCount[index];

                int obstaclePositionIndex;
                float obstacleDistance;
                NearestPosition(obstaclePositions, position, out obstaclePositionIndex, out obstacleDistance);
                cellObstaclePositionIndex[index] = obstaclePositionIndex;
                cellObstacleDistance[index]      = obstacleDistance;

                int targetPositionIndex;
                float targetDistance;
                NearestPosition(targetPositions, position, out targetPositionIndex, out targetDistance);
                cellTargetPositionIndex[index] = targetPositionIndex;

                cellIndices[index] = index;
            }
            
            // Sums the alignment and separation of the actual index being considered and stores
            // the index of this first value where we're storing the cells.
            public void ExecuteNext(int cellIndex, int index)
            {
                cellCount[cellIndex]      += 1;
                cellAlignment[cellIndex]  = cellAlignment[cellIndex] + cellAlignment[index];
                cellSeparation[cellIndex] = cellSeparation[cellIndex] + cellSeparation[index];
                cellIndices[index]        = cellIndex;
            }
        }

        // This reads the previously calculated boid information for all the Boids of each cell to update
        // the `localToWorld` of each of the boids based on their newly calculated headings using
        // the standard boids flocking algorithm.
        [BurstCompile]
        [RequireComponentTag(typeof(Boid))]
        struct Steer : IJobForEachWithEntity<LocalToWorld>
        {
            public float                                                       dt; 
            [ReadOnly] public Boid                                             settings;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int>     cellIndices;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3>  targetPositions;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3>  obstaclePositions;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3>  cellAlignment;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3>  cellSeparation;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int>     cellObstaclePositionIndex;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float>   cellObstacleDistance;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int>     cellTargetPositionIndex;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int>     cellCount;

            public void Execute(Entity entity, int index, ref LocalToWorld localToWorld)
            {
                // temporarily storing the values for code readability
                var forward                           = localToWorld.Forward;
                var currentPosition                   = localToWorld.Position;
                var cellIndex                         = cellIndices[index];
                var neighborCount                     = cellCount[cellIndex];
                var alignment                         = cellAlignment[cellIndex];
                var separation                        = cellSeparation[cellIndex];
                var nearestObstacleDistance           = cellObstacleDistance[cellIndex];
                var nearestObstaclePositionIndex      = cellObstaclePositionIndex[cellIndex];
                var nearestTargetPositionIndex        = cellTargetPositionIndex[cellIndex];
                var nearestObstaclePosition           = obstaclePositions[nearestObstaclePositionIndex];
                var nearestTargetPosition             = targetPositions[nearestTargetPositionIndex];

                // steering calculations based on the boids algorithm
                var obstacleSteering                  = currentPosition - nearestObstaclePosition;
                var avoidObstacleHeading              = (nearestObstaclePosition + math.normalizesafe(obstacleSteering)
                                                        * settings.ObstacleAversionDistance)- currentPosition;
                var targetHeading                     = settings.TargetWeight
                                                        * math.normalizesafe(nearestTargetPosition - currentPosition);
                var nearestObstacleDistanceFromRadius = nearestObstacleDistance - settings.ObstacleAversionDistance;
                var alignmentResult                   = settings.AlignmentWeight
                                                        * math.normalizesafe((alignment/neighborCount)-forward);
                var separationResult                  = settings.SeparationWeight
                                                        * math.normalizesafe((currentPosition * neighborCount) - separation);
                var normalHeading                     = math.normalizesafe(alignmentResult + separationResult + targetHeading);
                var targetForward                     = math.select(normalHeading, avoidObstacleHeading, nearestObstacleDistanceFromRadius < 0);
                var nextHeading                       = math.normalizesafe(forward + dt*(targetForward-forward));

                // updates based on the new heading
                localToWorld = new LocalToWorld
                {
                    Value = float4x4.TRS(
                        new float3(localToWorld.Position + (nextHeading * settings.MoveSpeed * dt)),
                        quaternion.LookRotationSafe(nextHeading, math.up()),
                        new float3(1.0f, 1.0f, 1.0f))
                };
            }
        }

        protected override void OnStopRunning()
        {
            for (var i = 0; i < m_PrevFrameHashmaps.Count; ++i)
            {
                m_PrevFrameHashmaps[i].Dispose();
            }
            m_PrevFrameHashmaps.Clear();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityManager.GetAllUniqueSharedComponentData(m_UniqueTypes);

            var obstacleCount = m_ObstacleQuery.CalculateEntityCount();
            var targetCount = m_TargetQuery.CalculateEntityCount(); 
            
            // Cannot call [DeallocateOnJobCompletion] on Hashmaps yet, so doing own cleanup here
            // of the hashes created in the previous iteration.
            for (int i = 0; i < m_PrevFrameHashmaps.Count; ++i)
            {
                m_PrevFrameHashmaps[i].Dispose();
            }
            m_PrevFrameHashmaps.Clear();

            // Each variant of the Boid represents a different value of the SharedComponentData and is self-contained,
            // meaning Boids of the same variant only interact with one another. Thus, this loop processes each
            // variant type individually.
            for (int boidVariantIndex = 0; boidVariantIndex < m_UniqueTypes.Count; boidVariantIndex++)
            {
                var settings = m_UniqueTypes[boidVariantIndex];
                m_BoidQuery.SetFilter(settings);
                var boidCount = m_BoidQuery.CalculateEntityCount();
                
                if (boidCount == 0)
                {
                    // Early out. If the given variant includes no Boids, move on to the next loop.
                    // For example, variant 0 will always exit early bc it's it represents a default, uninitialized
                    // Boid struct, which does not appear in this sample.
                    continue;
                }

                // The following calculates spatial cells of neighboring Boids
                // note: working with a sparse grid and not a dense bounded grid so there
                // are no predefined borders of the space.

                var hashMap                   = new NativeMultiHashMap<int,int>(boidCount,Allocator.TempJob);

                var cellIndices               = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellObstaclePositionIndex = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellTargetPositionIndex   = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                
                var cellCount                 = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var cellObstacleDistance      = new NativeArray<float>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellAlignment             = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellSeparation            = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                
                var copyTargetPositions       = new NativeArray<float3>(targetCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var copyObstaclePositions     = new NativeArray<float3>(obstacleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                
                // The following jobs all run in parallel because the same JobHandle is passed for their
                // input dependencies when the jobs are scheduled; thus, they can run in any order (or concurrently).
                // The concurrency is property of how they're scheduled, not of the job structs themselves.

                var initialCellAlignmentJob = new CopyHeadings
                {
                    headings = cellAlignment
                };
                var initialCellAlignmentJobHandle = initialCellAlignmentJob.Schedule(m_BoidQuery, inputDeps);

                var initialCellSeparationJob = new CopyPositions
                {
                    positions = cellSeparation
                };
                var initialCellSeparationJobHandle = initialCellSeparationJob.Schedule(m_BoidQuery, inputDeps);

                var copyTargetPositionsJob = new CopyPositions
                {
                    positions = copyTargetPositions
                };
                var copyTargetPositionsJobHandle = copyTargetPositionsJob.Schedule(m_TargetQuery, inputDeps);

                var copyObstaclePositionsJob = new CopyPositions
                {
                    positions = copyObstaclePositions
                };
                var copyObstaclePositionsJobHandle = copyObstaclePositionsJob.Schedule(m_ObstacleQuery, inputDeps);

                // Cannot call [DeallocateOnJobCompletion] on Hashmaps yet, so adding resolved hashes to the list
                // so that theyre usable in the upcoming cell jobs and also have a straight forward cleanup.
                m_PrevFrameHashmaps.Add(hashMap);

                // setting up the jobs for position and cell count
                
                var hashPositionsJob = new HashPositions
                {
                    hashMap        = hashMap.AsParallelWriter(),
                    cellRadius     = settings.CellRadius
                };
                var hashPositionsJobHandle = hashPositionsJob.Schedule(m_BoidQuery, inputDeps);

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
                    cellTargetPositionIndex   = cellTargetPositionIndex,
                    cellCount                 = cellCount,
                    targetPositions           = copyTargetPositions,
                    obstaclePositions         = copyObstaclePositions
                };
                var mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap,64,mergeCellsBarrierJobHandle);

                var steerJob = new Steer
                {
                    cellIndices               = cellIndices,
                    settings                  = settings,
                    cellAlignment             = cellAlignment,
                    cellSeparation            = cellSeparation,
                    cellObstacleDistance      = cellObstacleDistance,
                    cellObstaclePositionIndex = cellObstaclePositionIndex,
                    cellTargetPositionIndex   = cellTargetPositionIndex,
                    cellCount                 = cellCount,
                    targetPositions           = copyTargetPositions,
                    obstaclePositions         = copyObstaclePositions,
                    dt                        = Time.deltaTime,
                };
                var steerJobHandle = steerJob.Schedule(m_BoidQuery, mergeCellsJobHandle);

                inputDeps = steerJobHandle;
                m_BoidQuery.AddDependency(inputDeps);
            }
            m_UniqueTypes.Clear();

            return inputDeps;
        }

        protected override void OnCreate()
        {
            m_BoidQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new [] { ComponentType.ReadOnly<Boid>(), ComponentType.ReadWrite<LocalToWorld>() },
            });

            m_TargetQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new [] { ComponentType.ReadOnly<BoidTarget>(), ComponentType.ReadOnly<LocalToWorld>() },
            });
            
            m_ObstacleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new [] { ComponentType.ReadOnly<BoidObstacle>(), ComponentType.ReadOnly<LocalToWorld>() },
            });
        }
    }
}
