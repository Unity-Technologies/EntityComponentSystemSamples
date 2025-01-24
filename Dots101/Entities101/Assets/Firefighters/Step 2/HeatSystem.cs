using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Tutorials.Firefighters
{
    public partial struct HeatSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<GroundCell>();
            state.RequireForUpdate<Heat>();
            state.RequireForUpdate<ExecuteHeat>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            DynamicBuffer<Heat> heatBuffer = SystemAPI.GetSingletonBuffer<Heat>(false);

            // simulate the heat spreading
            {
                //HeatSpread_MainThread(ref state, heatBuffer, config);
                //state.Dependency = HeatSpread_SingleThreadedJob(state.Dependency, ref state, heatBuffer, config);
                state.Dependency = HeatSpread_ParallelJob(state.Dependency, ref state, heatBuffer, config);
            }

            // update the colors and heights of the ground cells from the heat data
            {
                //GroundCellUpdate_MainThread(ref state, heatBuffer, config);
                //state.Dependency = GroundCellUpdate_SingleThreadedJob(state.Dependency, ref state, heatBuffer, config);
                state.Dependency = GroundCellUpdate_ParallelJob(state.Dependency, ref state, heatBuffer, config);
            }
        }


        private void HeatSpread_MainThread(ref SystemState state, DynamicBuffer<Heat> heatBuffer, Config config)
        {
            var heatRW = heatBuffer;
            NativeArray<Heat> heatRO = heatBuffer.ToNativeArray(Allocator.Temp);
            var speed = SystemAPI.Time.DeltaTime * config.HeatSpreadSpeed;
            int numColumns = config.GroundNumColumns;
            int numRows = config.GroundNumRows;

            for (int index = 0; index < heatRO.Length; index++)
            {
                int row = index / numColumns;
                int col = index % numRows;

                var prevCol = col - 1;
                var nextCol = col + 1;
                var prevRow = row - 1;
                var nextRow = row + 1;

                float increase = 0;

                increase += Index(row, nextCol, heatRO, numColumns, numRows);
                increase += Index(row, prevCol, heatRO, numColumns, numRows);

                increase += Index(prevRow, prevCol, heatRO, numColumns, numRows);
                increase += Index(prevRow, col, heatRO, numColumns, numRows);
                increase += Index(prevRow, nextCol, heatRO, numColumns, numRows);

                increase += Index(nextRow, prevCol, heatRO, numColumns, numRows);
                increase += Index(nextRow, col, heatRO, numColumns, numRows);
                increase += Index(nextRow, nextCol, heatRO, numColumns, numRows);

                increase *= speed;

                heatRW[index] = new Heat
                {
                    Value = math.min(1, heatRO[index].Value + increase)
                };
            }
        }

        private static float Index(int row, int col, NativeArray<Heat> heatRO, int numColumns, int numRows)
        {
            if (col < 0 || col >= numColumns ||
                row < 0 || row >= numRows)
            {
                return 0;
            }

            return heatRO[row * numColumns + col].Value;
        }

        private JobHandle HeatSpread_SingleThreadedJob(JobHandle dependency, ref SystemState state,
            DynamicBuffer<Heat> heatBuffer, Config config)
        {
            var heatSpreadJob = new HeatSpreadJob_SingleThreaded()
            {
                heatRW = heatBuffer.AsNativeArray(),
                heatRO = heatBuffer.ToNativeArray(state.WorldUpdateAllocator),
                HeatSpreadSpeed = SystemAPI.Time.DeltaTime * config.HeatSpreadSpeed,
                NumColumns = config.GroundNumColumns,
                NumRows = config.GroundNumRows
            };
            return heatSpreadJob.Schedule(dependency);
        }

        [BurstCompile]
        public struct HeatSpreadJob_SingleThreaded : IJob
        {
            public NativeArray<Heat> heatRW;
            [ReadOnly] public NativeArray<Heat> heatRO;
            public float HeatSpreadSpeed;
            public int NumColumns;
            public int NumRows;

            public void Execute()
            {
                for (int index = 0; index < heatRO.Length; index++)
                {
                    int row = index / NumColumns;
                    int col = index % NumColumns;

                    var prevCol = col - 1;
                    var nextCol = col + 1;
                    var prevRow = row - 1;
                    var nextRow = row + 1;

                    float increase = 0;

                    increase += Index(row, nextCol);
                    increase += Index(row, prevCol);

                    increase += Index(prevRow, prevCol);
                    increase += Index(prevRow, col);
                    increase += Index(prevRow, nextCol);

                    increase += Index(nextRow, prevCol);
                    increase += Index(nextRow, col);
                    increase += Index(nextRow, nextCol);

                    increase *= HeatSpreadSpeed;

                    heatRW[index] = new Heat
                    {
                        Value = math.min(1, heatRO[index].Value + increase)
                    };
                }
            }

            private float Index(int row, int col)
            {
                if (col < 0 || col >= NumColumns ||
                    row < 0 || row >= NumRows)
                {
                    return 0;
                }

                return heatRO[row * NumColumns + col].Value;
            }
        }

        private JobHandle HeatSpread_ParallelJob(JobHandle dependency, ref SystemState state,
            DynamicBuffer<Heat> heatBuffer, Config config)
        {
            var heatSpreadJob = new HeatSpreadJob_Parallel
            {
                heatRW = heatBuffer.AsNativeArray(),
                heatRO = heatBuffer.ToNativeArray(state.WorldUpdateAllocator),
                HeatSpreadSpeed = SystemAPI.Time.DeltaTime * config.HeatSpreadSpeed,
                NumColumns = config.GroundNumColumns,
                NumRows = config.GroundNumRows
            };
            return heatSpreadJob.Schedule(heatBuffer.Length, 100, dependency);
        }

        [BurstCompile]
        public struct HeatSpreadJob_Parallel : IJobParallelFor
        {
            public NativeArray<Heat> heatRW;
            [ReadOnly] public NativeArray<Heat> heatRO;
            public float HeatSpreadSpeed;
            public int NumColumns;
            public int NumRows;

            public void Execute(int index)
            {
                int row = index / NumColumns;
                int col = index % NumColumns;

                var prevCol = col - 1;
                var nextCol = col + 1;
                var prevRow = row - 1;
                var nextRow = row + 1;

                float increase = 0;

                increase += Index(row, nextCol);
                increase += Index(row, prevCol);

                increase += Index(prevRow, prevCol);
                increase += Index(prevRow, col);
                increase += Index(prevRow, nextCol);

                increase += Index(nextRow, prevCol);
                increase += Index(nextRow, col);
                increase += Index(nextRow, nextCol);

                increase *= HeatSpreadSpeed;

                heatRW[index] = new Heat
                {
                    Value = math.min(1, heatRO[index].Value + increase)
                };
            }

            private float Index(int row, int col)
            {
                if (col < 0 || col >= NumColumns ||
                    row < 0 || row >= NumRows)
                {
                    return 0;
                }

                return heatRO[row * NumColumns + col].Value;
            }
        }


        private void GroundCellUpdate_MainThread(ref SystemState state, DynamicBuffer<Heat> heatBuffer, Config config)
        {
            int idx = 0;
            var minY = -(config.GroundCellYScale / 2);
            var maxY = minY + config.GroundCellYScale;
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (trans, color) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<URPMaterialPropertyBaseColor>>()
                         .WithAll<GroundCell>())
            {
                var heat = heatBuffer[idx].Value;

                // oscillate the displayed heat so that the fire looks a little more organic
                {
                    var radians = Random.CreateFromIndex((uint)idx).NextFloat(math.PI * 2) + elapsedTime;
                    var oscillationOffset =
                        math.sin(radians) * heat * config.HeatOscillationScale; // the more heat, the more oscillation
                    heat += oscillationOffset;
                }

                trans.ValueRW.Position.y = math.lerp(minY, maxY, heat);
                color.ValueRW.Value = math.lerp(config.MinHeatColor, config.MaxHeatColor, heat);

                idx++;
            }
        }

        private JobHandle GroundCellUpdate_SingleThreadedJob(JobHandle dependency, ref SystemState state,
            DynamicBuffer<Heat> heatBuffer, Config config)
        {
            var minY = -(config.GroundCellYScale / 2);
            var groundCellUpdateJob = new GroundCellUpdate
            {
                Config = config,
                HeatBuffer = heatBuffer,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                MinY = minY,
                MaxY = minY + config.GroundCellYScale,
            };

            return groundCellUpdateJob.Schedule(dependency);
        }

        private JobHandle GroundCellUpdate_ParallelJob(JobHandle dependency, ref SystemState state,
            DynamicBuffer<Heat> heatBuffer, Config config)
        {
            var minY = -(config.GroundCellYScale / 2);
            var groundCellUpdateJob = new GroundCellUpdate
            {
                Config = config,
                HeatBuffer = heatBuffer,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                MinY = minY,
                MaxY = minY + config.GroundCellYScale,
            };

            return groundCellUpdateJob.ScheduleParallel(dependency);
        }

        [WithAll(typeof(GroundCell))]
        [BurstCompile]
        public partial struct GroundCellUpdate : IJobEntity
        {
            public float ElapsedTime;
            public Config Config;
            [ReadOnly] public DynamicBuffer<Heat> HeatBuffer;
            public float MinY;
            public float MaxY;

            public void Execute(ref LocalTransform trans, ref URPMaterialPropertyBaseColor color, [EntityIndexInQuery] int entityIdx)
            {
                var heat = HeatBuffer[entityIdx].Value;

                // oscillate the displayed heat so that the fire looks a little more organic
                {
                    var radians = Random.CreateFromIndex((uint)entityIdx).NextFloat(math.PI * 2) + ElapsedTime;
                    var oscillationOffset =
                        math.sin(radians) * heat * Config.HeatOscillationScale; // the more heat, the more oscillation
                    heat += oscillationOffset;
                }

                trans.Position.y = math.lerp(MinY, MaxY, heat);
                color.Value = math.lerp(Config.MinHeatColor, Config.MaxHeatColor, heat);

                entityIdx++;
            }
        }

        // douse a cell and all surrounding cells
        public static void DouseFire(float2 location, DynamicBuffer<Heat> heatBuffer, int numRows, int numCols)
        {
            int col = (int)location.x;
            int row = (int)location.y;

            DouseCell(row, col, heatBuffer, numRows, numCols);
            DouseCell(row, col + 1, heatBuffer, numRows, numCols);
            DouseCell(row, col - 1, heatBuffer, numRows, numCols);
            DouseCell(row - 1, col, heatBuffer, numRows, numCols);
            DouseCell(row - 1, col + 1, heatBuffer, numRows, numCols);
            DouseCell(row - 1, col - 1, heatBuffer, numRows, numCols);
            DouseCell(row + 1, col, heatBuffer, numRows, numCols);
            DouseCell(row + 1, col + 1, heatBuffer, numRows, numCols);
            DouseCell(row + 1, col - 1, heatBuffer, numRows, numCols);
        }

        private static void DouseCell(int row, int col, DynamicBuffer<Heat> heatBuffer, int numRows, int numCols)
        {
            if (col < 0 || col >= numCols ||
                row < 0 || row >= numRows)
            {
                return;
            }

            heatBuffer[row * numCols + col] = new Heat { Value = 0 };
        }

        public static float2 NearestFire(float2 location, DynamicBuffer<Heat> heatBuffer, int numRows, int numCols, float minHeat)
        {
            var closestFirePos = new float2(0.5f, 0.5f);
            var closestDistSq = float.MaxValue;

            // check every cell
            for (int col = 0; col < numCols; col++)
            {
                for (int row = 0; row < numRows; row++)
                {
                    if (heatBuffer[row * numCols + col].Value > minHeat) // is cell on fire
                    {
                        var firePos = new float2(col + 0.5f, row + 0.5f);
                        var distSq = math.distancesq(location, firePos);

                        if (distSq < closestDistSq)
                        {
                            closestFirePos = firePos;
                            closestDistSq = distSq;
                        }
                    }
                }
            }

            return closestFirePos;
        }
    }
}
