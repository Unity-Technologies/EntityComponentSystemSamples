using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

// Mike's GDC Talk on 'A Data Oriented Approach to Using Component Systems'
// is a great reference for dissecting the Boids sample code:
// https://youtu.be/p65Yt20pw0g?t=1446
// It explains a slightly older implementation of this sample but almost all the
// information is still relevant.

// The targets (2 red fish) and obstacle (1 shark) move based on the ActorAnimation tab
// in the Unity UI, so that they are moving based on key-framed animation.

namespace Boids
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct BoidSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var boidQuery = SystemAPI.QueryBuilder().WithAll<Boid>().WithAllRW<LocalToWorld>().Build();
            var targetQuery = SystemAPI.QueryBuilder().WithAll<BoidTarget, LocalToWorld>().Build();
            var obstacleQuery = SystemAPI.QueryBuilder().WithAll<BoidObstacle, LocalToWorld>().Build();

            var obstacleCount = obstacleQuery.CalculateEntityCount();
            var targetCount = targetQuery.CalculateEntityCount();

            var world = state.WorldUnmanaged;
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<Boid> uniqueBoidTypes, world.UpdateAllocator.ToAllocator);
            float dt = math.min(0.05f, SystemAPI.Time.DeltaTime);

            // Each variant of the Boid represents a different value of the SharedComponentData and is self-contained,
            // meaning Boids of the same variant only interact with one another. Thus, this loop processes each
            // variant type individually.
            foreach (var boidSettings in uniqueBoidTypes)
            {
                boidQuery.AddSharedComponentFilter(boidSettings);

                var boidCount = boidQuery.CalculateEntityCount();
                if (boidCount == 0)
                {
                    // Early out. If the given variant includes no Boids, move on to the next loop.
                    // For example, variant 0 will always exit early bc it's it represents a default, uninitialized
                    // Boid struct, which does not appear in this sample.
                    boidQuery.ResetFilter();
                    continue;
                }

                // The following calculates spatial cells of neighboring Boids
                // note: working with a sparse grid and not a dense bounded grid so there
                // are no predefined borders of the space.

                var hashMap                   = new NativeParallelMultiHashMap<int, int>(boidCount, world.UpdateAllocator.ToAllocator);
                var cellIndices               = CollectionHelper.CreateNativeArray<int, RewindableAllocator>(boidCount, ref world.UpdateAllocator);
                var cellObstaclePositionIndex = CollectionHelper.CreateNativeArray<int, RewindableAllocator>(boidCount, ref world.UpdateAllocator);
                var cellTargetPositionIndex   = CollectionHelper.CreateNativeArray<int, RewindableAllocator>(boidCount, ref world.UpdateAllocator);
                var cellCount                 = CollectionHelper.CreateNativeArray<int, RewindableAllocator>(boidCount, ref world.UpdateAllocator);
                var cellObstacleDistance      = CollectionHelper.CreateNativeArray<float, RewindableAllocator>(boidCount, ref world.UpdateAllocator);
                var cellAlignment             = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(boidCount, ref world.UpdateAllocator);
                var cellSeparation            = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(boidCount, ref world.UpdateAllocator);

                var copyTargetPositions       = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(targetCount, ref world.UpdateAllocator);
                var copyObstaclePositions     = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(obstacleCount, ref world.UpdateAllocator);

                // The following jobs all run in parallel because the same JobHandle is passed for their
                // input dependencies when the jobs are scheduled; thus, they can run in any order (or concurrently).
                // The concurrency is property of how they're scheduled, not of the job structs themselves.

                var boidChunkBaseEntityIndexArray = boidQuery.CalculateBaseEntityIndexArrayAsync(
                    world.UpdateAllocator.ToAllocator, state.Dependency,
                    out var boidChunkBaseIndexJobHandle);
                var targetChunkBaseEntityIndexArray = targetQuery.CalculateBaseEntityIndexArrayAsync(
                    world.UpdateAllocator.ToAllocator, state.Dependency,
                    out var targetChunkBaseIndexJobHandle);
                var obstacleChunkBaseEntityIndexArray = obstacleQuery.CalculateBaseEntityIndexArrayAsync(
                    world.UpdateAllocator.ToAllocator, state.Dependency,
                    out var obstacleChunkBaseIndexJobHandle);
                // These jobs extract the relevant position, heading component
                // to NativeArrays so that they can be randomly accessed by the `MergeCells` and `Steer` jobs.
                // These jobs are defined using the IJobEntity syntax.
                var initialBoidJob = new InitialPerBoidJob
                {
                    ChunkBaseEntityIndices = boidChunkBaseEntityIndexArray,
                    CellAlignment = cellAlignment,
                    CellSeparation = cellSeparation,
                    ParallelHashMap = hashMap.AsParallelWriter(),
                    InverseBoidCellRadius = 1.0f / boidSettings.CellRadius,
                };
                var initialBoidJobHandle = initialBoidJob.ScheduleParallel(boidQuery, boidChunkBaseIndexJobHandle);

                var initialTargetJob = new InitialPerTargetJob
                {
                    ChunkBaseEntityIndices = targetChunkBaseEntityIndexArray,
                    TargetPositions = copyTargetPositions,
                };
                var initialTargetJobHandle = initialTargetJob.ScheduleParallel(targetQuery, targetChunkBaseIndexJobHandle);

                var initialObstacleJob = new InitialPerObstacleJob
                {
                    ChunkBaseEntityIndices = obstacleChunkBaseEntityIndexArray,
                    ObstaclePositions = copyObstaclePositions,
                };
                var initialObstacleJobHandle = initialObstacleJob.ScheduleParallel(obstacleQuery, obstacleChunkBaseIndexJobHandle);

                var initialCellCountJob = new MemsetNativeArray<int>
                {
                    Source = cellCount,
                    Value  = 1
                };
                var initialCellCountJobHandle = initialCellCountJob.Schedule(boidCount, 64, state.Dependency);

                var initialCellBarrierJobHandle = JobHandle.CombineDependencies(initialBoidJobHandle, initialCellCountJobHandle);
                var copyTargetObstacleBarrierJobHandle = JobHandle.CombineDependencies(initialTargetJobHandle, initialObstacleJobHandle);
                var mergeCellsBarrierJobHandle = JobHandle.CombineDependencies(initialCellBarrierJobHandle, copyTargetObstacleBarrierJobHandle);

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
                var mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap, 64, mergeCellsBarrierJobHandle);

                // This reads the previously calculated boid information for all the boids of each cell to update
                // the `localToWorld` of each of the boids based on their newly calculated headings using
                // the standard boid flocking algorithm.
                var steerBoidJob = new SteerBoidJob
                {
                    ChunkBaseEntityIndices = boidChunkBaseEntityIndexArray,
                    CellIndices = cellIndices,
                    CellCount = cellCount,
                    CellAlignment = cellAlignment,
                    CellSeparation = cellSeparation,
                    CellObstacleDistance = cellObstacleDistance,
                    CellObstaclePositionIndex = cellObstaclePositionIndex,
                    CellTargetPositionIndex = cellTargetPositionIndex,
                    ObstaclePositions = copyObstaclePositions,
                    TargetPositions = copyTargetPositions,
                    CurrentBoidVariant = boidSettings,
                    DeltaTime = dt,
                    MoveDistance = boidSettings.MoveSpeed * dt,
                };
                var steerBoidJobHandle = steerBoidJob.ScheduleParallel(boidQuery, mergeCellsJobHandle);

                // Dispose allocated containers with dispose jobs.
                state.Dependency = steerBoidJobHandle;

                // We pass the job handle and add the dependency so that we keep the proper ordering between the jobs
                // as the looping iterates. For our purposes of execution, this ordering isn't necessary; however, without
                // the add dependency call here, the safety system will throw an error, because we're accessing multiple
                // pieces of boid data and it would think there could possibly be a race condition.

                boidQuery.AddDependency(state.Dependency);
                boidQuery.ResetFilter();
            }
            uniqueBoidTypes.Dispose();
        }

                // In this sample there are 3 total unique boid variants, one for each unique value of the
        // Boid SharedComponent (note: this includes the default uninitialized value at
        // index 0, which isnt actually used in the sample).

        // This accumulates the `positions` (separations) and `headings` (alignments) of all the boids in each cell to:
        // 1) count the number of boids in each cell
        // 2) find the nearest obstacle and target to each boid cell
        // 3) track which array entry contains the accumulated values for each boid's cell
        // In this context, the cell represents the hashed bucket of boids that are near one another within cellRadius
        // floored to the nearest int3.
        // Note: `IJobNativeParallelMultiHashMapMergedSharedKeyIndices` is a custom job to iterate safely/efficiently over the
        // NativeContainer used in this sample (`NativeParallelMultiHashMap`). Currently these kinds of changes or additions of
        // custom jobs generally require access to data/fields that aren't available through the `public` API of the
        // containers. This is why the custom job type `IJobNativeParallelMultiHashMapMergedSharedKeyIndicies` is declared in
        // the DOTS package (which can see the `internal` container fields) and not in the Boids sample.
        [BurstCompile]
        struct MergeCells : IJobNativeParallelMultiHashMapMergedSharedKeyIndices
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

            void NearestPosition(NativeArray<float3> targets, float3 position, out int nearestPositionIndex, out float nearestDistance)
            {
                nearestPositionIndex = 0;
                nearestDistance      = math.lengthsq(position - targets[0]);
                for (int i = 1; i < targets.Length; i++)
                {
                    var targetPosition = targets[i];
                    var distance       = math.lengthsq(position - targetPosition);
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
            // note: these items are summed so that in `Steer` their average for the cell can be resolved.
            public void ExecuteNext(int cellIndex, int index)
            {
                cellCount[cellIndex]      += 1;
                cellAlignment[cellIndex]  += cellAlignment[cellIndex];
                cellSeparation[cellIndex] += cellSeparation[cellIndex];
                cellIndices[index]        = cellIndex;
            }
        }

        [BurstCompile]
        partial struct InitialPerBoidJob : IJobEntity
        {
            [ReadOnly] public NativeArray<int> ChunkBaseEntityIndices;
            [NativeDisableParallelForRestriction] public NativeArray<float3> CellAlignment;
            [NativeDisableParallelForRestriction] public NativeArray<float3> CellSeparation;
            public NativeParallelMultiHashMap<int, int>.ParallelWriter ParallelHashMap;
            public float InverseBoidCellRadius;
            void Execute([ChunkIndexInQuery] int chunkIndexInQuery, [EntityIndexInChunk] int entityIndexInChunk, in LocalToWorld localToWorld)
            {
                int entityIndexInQuery = ChunkBaseEntityIndices[chunkIndexInQuery] + entityIndexInChunk;
                CellAlignment[entityIndexInQuery] = localToWorld.Forward;
                CellSeparation[entityIndexInQuery] = localToWorld.Position;
                // Populates a hash map, where each bucket contains the indices of all Boids whose positions quantize
                // to the same value for a given cell radius so that the information can be randomly accessed by
                // the `MergeCells` and `Steer` jobs.
                // This is useful in terms of the algorithm because it limits the number of comparisons that will
                // actually occur between the different boids. Instead of for each boid, searching through all
                // boids for those within a certain radius, this limits those by the hash-to-bucket simplification.
                var hash = (int)math.hash(new int3(math.floor(localToWorld.Position * InverseBoidCellRadius)));
                ParallelHashMap.Add(hash, entityIndexInQuery);
            }
        }

        [BurstCompile]
        partial struct InitialPerTargetJob : IJobEntity
        {
            [ReadOnly] public NativeArray<int> ChunkBaseEntityIndices;
            [NativeDisableParallelForRestriction] public NativeArray<float3> TargetPositions;
            void Execute([ChunkIndexInQuery] int chunkIndexInQuery, [EntityIndexInChunk] int entityIndexInChunk, in LocalToWorld localToWorld)
            {
                int entityIndexInQuery = ChunkBaseEntityIndices[chunkIndexInQuery] + entityIndexInChunk;
                TargetPositions[entityIndexInQuery] = localToWorld.Position;
            }
        }

        [BurstCompile]
        partial struct InitialPerObstacleJob : IJobEntity
        {
            [ReadOnly] public NativeArray<int> ChunkBaseEntityIndices;
            [NativeDisableParallelForRestriction] public NativeArray<float3> ObstaclePositions;
            void Execute([ChunkIndexInQuery] int chunkIndexInQuery, [EntityIndexInChunk] int entityIndexInChunk, in LocalToWorld localToWorld)
            {
                int entityIndexInQuery = ChunkBaseEntityIndices[chunkIndexInQuery] + entityIndexInChunk;
                ObstaclePositions[entityIndexInQuery] = localToWorld.Position;
            }
        }

        [BurstCompile]
        partial struct SteerBoidJob : IJobEntity
        {
            [ReadOnly] public NativeArray<int> ChunkBaseEntityIndices;
            [ReadOnly] public NativeArray<int> CellIndices;
            [ReadOnly] public NativeArray<int> CellCount;
            [ReadOnly] public NativeArray<float3> CellAlignment;
            [ReadOnly] public NativeArray<float3> CellSeparation;
            [ReadOnly] public NativeArray<float> CellObstacleDistance;
            [ReadOnly] public NativeArray<int> CellObstaclePositionIndex;
            [ReadOnly] public NativeArray<int> CellTargetPositionIndex;
            [ReadOnly] public NativeArray<float3> ObstaclePositions;
            [ReadOnly] public NativeArray<float3> TargetPositions;
            public Boid CurrentBoidVariant;
            public float DeltaTime;
            public float MoveDistance;
            void Execute([ChunkIndexInQuery] int chunkIndexInQuery, [EntityIndexInChunk] int entityIndexInChunk, ref LocalToWorld localToWorld)
            {
                int entityIndexInQuery = ChunkBaseEntityIndices[chunkIndexInQuery] + entityIndexInChunk;
                // temporarily storing the values for code readability
                var forward                           = localToWorld.Forward;
                var currentPosition                   = localToWorld.Position;
                var cellIndex                         = CellIndices[entityIndexInQuery];
                var neighborCount                     = CellCount[cellIndex];
                var alignment                         = CellAlignment[cellIndex];
                var separation                        = CellSeparation[cellIndex];
                var nearestObstacleDistance           = CellObstacleDistance[cellIndex];
                var nearestObstaclePositionIndex      = CellObstaclePositionIndex[cellIndex];
                var nearestTargetPositionIndex        = CellTargetPositionIndex[cellIndex];
                var nearestObstaclePosition           = ObstaclePositions[nearestObstaclePositionIndex];
                var nearestTargetPosition             = TargetPositions[nearestTargetPositionIndex];

                // Setting up the directions for the three main biocrowds influencing directions adjusted based
                // on the predefined weights:
                // 1) alignment - how much should it move in a direction similar to those around it?
                // note: we use `alignment/neighborCount`, because we need the average alignment in this case; however
                // alignment is currently the summation of all those of the boids within the cellIndex being considered.
                var alignmentResult     = CurrentBoidVariant.AlignmentWeight
                                          * math.normalizesafe((alignment / neighborCount) - forward);
                // 2) separation - how close is it to other boids and are there too many or too few for comfort?
                // note: here separation represents the summed possible center of the cell. We perform the multiplication
                // so that both `currentPosition` and `separation` are weighted to represent the cell as a whole and not
                // the current individual boid.
                var separationResult    = CurrentBoidVariant.SeparationWeight
                                          * math.normalizesafe((currentPosition * neighborCount) - separation);
                // 3) target - is it still towards its destination?
                var targetHeading       = CurrentBoidVariant.TargetWeight
                                          * math.normalizesafe(nearestTargetPosition - currentPosition);

                // creating the obstacle avoidant vector s.t. it's pointing towards the nearest obstacle
                // but at the specified 'ObstacleAversionDistance'. If this distance is greater than the
                // current distance to the obstacle, the direction becomes inverted. This simulates the
                // idea that if `currentPosition` is too close to an obstacle, the weight of this pushes
                // the current boid to escape in the fastest direction; however, if the obstacle isn't
                // too close, the weighting denotes that the boid doesnt need to escape but will move
                // slower if still moving in that direction (note: we end up not using this move-slower
                // case, because of `targetForward`'s decision to not use obstacle avoidance if an obstacle
                // isn't close enough).
                var obstacleSteering                  = currentPosition - nearestObstaclePosition;
                var avoidObstacleHeading              = (nearestObstaclePosition + math.normalizesafe(obstacleSteering)
                    * CurrentBoidVariant.ObstacleAversionDistance) - currentPosition;

                // the updated heading direction. If not needing to be avoidant (ie obstacle is not within
                // predefined radius) then go with the usual defined heading that uses the amalgamation of
                // the weighted alignment, separation, and target direction vectors.
                var nearestObstacleDistanceFromRadius = nearestObstacleDistance - CurrentBoidVariant.ObstacleAversionDistance;
                var normalHeading                     = math.normalizesafe(alignmentResult + separationResult + targetHeading);
                var targetForward                     = math.select(normalHeading, avoidObstacleHeading, nearestObstacleDistanceFromRadius < 0);

                // updates using the newly calculated heading direction
                var nextHeading                       = math.normalizesafe(forward + DeltaTime * (targetForward - forward));
                localToWorld = new LocalToWorld
                {
                    Value = float4x4.TRS(
                        // TODO: precalc speed*dt
                        new float3(localToWorld.Position + (nextHeading * MoveDistance)),
                        quaternion.LookRotationSafe(nextHeading, math.up()),
                        new float3(1.0f, 1.0f, 1.0f))
                };
            }
        }
    }
}
