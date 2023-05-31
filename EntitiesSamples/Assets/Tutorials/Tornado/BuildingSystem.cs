using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Tutorials.Tornado
{
    /*
     * Updates the bars and joints of the buildings.
     * The force of the tornado breaks the joints.
     */
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct BuildingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<PointArrays>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var pointData = SystemAPI.GetSingleton<PointArrays>();
            var time = (float)SystemAPI.Time.ElapsedTime;

            state.Dependency = new PointUpdateJob
            {
                config = config,
                currentPoints = pointData.current,
                previousPoints = pointData.previous,
                connectivity = pointData.connectivity,
                time = time,
                randomSeed = Random.CreateFromIndex(state.GlobalSystemVersion).NextUInt(),
                tornadoFader = math.saturate(time / 10),
                tornadoPosition = Position(time),
            }.Schedule(pointData.count.Value, 64, state.Dependency);

            state.EntityManager.GetAllUniqueSharedComponents<BarCluster>(out var clusters, Allocator.Temp);

            var barQuery = SystemAPI.QueryBuilder().WithAll<Bar, BarCluster>().Build();
            var barDataHandle = SystemAPI.GetComponentTypeHandle<Bar>();
            var dependencies = new NativeArray<JobHandle>(clusters.Length, Allocator.Temp);

            var barJob = new BarUpdateJob
            {
                config = config,
                barDataTypeHandle = barDataHandle,
                current = pointData.current,
                previous = pointData.previous,
                connectivity = pointData.connectivity,
                counter = pointData.count
            };

            for (int i = 0; i < clusters.Length; i++)
            {
                barQuery.SetSharedComponentFilter(clusters[i]);
                dependencies[i] = barJob.Schedule(barQuery, state.Dependency);
            }

            state.Dependency = JobHandle.CombineDependencies(dependencies);
        }

        public static float TornadoSway(float y, float time)
        {
            return math.sin(y / 5f + time / 4f) * 3f;
        }


        public static float2 Position(float time)
        {
            return new float2(math.cos(time / 6f), math.sin(time / 6f * 1.618f)) * 30f;
        }
    }

    [BurstCompile]
    struct PointUpdateJob : IJobParallelFor
    {
        public Config config;
        public NativeArray<float3> currentPoints;
        public NativeArray<float3> previousPoints;
        public NativeArray<byte> connectivity;
        public float time;
        public uint randomSeed;
        public float tornadoFader;
        public float2 tornadoPosition;

        public void Execute(int i)
        {
            if (connectivity[i] == byte.MaxValue) return;

            var point = currentPoints[i];
            var start = point;

            var previous = previousPoints[i];
            previous.y += .01f;

            // tornado force
            float tdx = tornadoPosition.x + BuildingSystem.TornadoSway(point.y, time) - point.x;
            float tdz = tornadoPosition.y - point.z;
            float tornadoDist = math.sqrt(tdx * tdx + tdz * tdz);
            tdx /= tornadoDist;
            tdz /= tornadoDist;
            if (tornadoDist < config.TornadoMaxForceDist)
            {
                float force = 1f - tornadoDist / config.TornadoMaxForceDist;
                float yFader = math.saturate(1f - point.y / config.TornadoHeight);
                force *= tornadoFader * config.TornadoForce *
                         Random.CreateFromIndex(randomSeed ^ (uint)i).NextFloat(-.3f, 1.3f);

                var forceVec = new float3
                {
                    x = -tdz + tdx * config.TornadoInwardForce * yFader,
                    y = config.TornadoUpForce,
                    z = tdx + tdz * config.TornadoInwardForce * yFader,
                };

                previous -= forceVec * force;
            }

            point += (point - previous) * (1 - config.BarDamping);

            previous = start;
            if (point.y < 0f)
            {
                point.y = 0f;
                previous.y = -previous.y;
                previous.x += (point.x - previous.x) * config.BarFriction;
                previous.z += (point.z - previous.z) * config.BarFriction;
            }

            previousPoints[i] = previous;
            currentPoints[i] = point;
        }
    }

    [BurstCompile]
    struct BarUpdateJob : IJobChunk
    {
        public Config config;

        [NativeDisableContainerSafetyRestriction]
        public ComponentTypeHandle<Bar> barDataTypeHandle;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> current;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<float3> previous;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<byte> connectivity;

        [NativeDisableContainerSafetyRestriction]
        public NativeReference<int> counter;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var barData = chunk.GetNativeArray(ref barDataTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var bar = barData[i];
                var iA = bar.pointA;
                var iB = bar.pointB;
                var currentA = current[iA];
                var currentB = current[iB];
                var anchorA = connectivity[iA] == byte.MaxValue;
                var anchorB = connectivity[iB] == byte.MaxValue;

                var d = currentB - currentA;

                float dist = math.length(d);
                float extraDist = dist - bar.length;

                var push = d / dist * extraDist;

                if (!anchorA && !anchorB)
                {
                    currentA += push / 2;
                    currentB -= push / 2;
                }
                else if (anchorA)
                {
                    currentB -= push;
                }
                else if (anchorB)
                {
                    currentA += push;
                }

                current[bar.pointA] = currentA;
                current[bar.pointB] = currentB;

                if (math.abs(extraDist) > config.BarBreakResistance)
                {
                    if (connectivity[iB] > 1 && !anchorB)
                    {
                        bar.pointB = DuplicatePoint(iB);
                    }
                    else if (connectivity[iA] > 1 && !anchorA)
                    {
                        bar.pointA = DuplicatePoint(iA);
                    }
                }

                barData[i] = bar;
            }
        }

        int DuplicatePoint(int index)
        {
            var newIdx = counter.AtomicAdd(1);
            connectivity[index] = (byte)(connectivity[index] - 1);

            connectivity[newIdx] = 1;
            current[newIdx] = current[index];
            previous[newIdx] = previous[index];
            return newIdx;
        }
    }
}
