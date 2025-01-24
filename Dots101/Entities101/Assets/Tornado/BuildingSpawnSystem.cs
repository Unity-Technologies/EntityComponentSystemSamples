using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Scenes;

namespace Tutorials.Tornado
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    public partial struct BuildingSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var random = Random.CreateFromIndex(1234);

            var points = new NativeList<float3>(Allocator.Temp);
            var connectivity = new NativeList<byte>(Allocator.Temp);

            // buildings
            for (int i = 0; i < 35; i++)
            {
                int height = random.NextInt(4, 12);
                var pos = new float3(random.NextFloat(-45f, 45f), 0f, random.NextFloat(-45f, 45f));
                float spacing = 2f;
                var anchor = byte.MaxValue;
                for (int j = 0; j < height; j++)
                {
                    points.Add(new float3(pos.x + spacing, j * spacing, pos.z - spacing));
                    connectivity.Add(anchor);

                    points.Add(new float3(pos.x - spacing, j * spacing, pos.z - spacing));
                    connectivity.Add(anchor);

                    points.Add(new float3(pos.x, j * spacing, pos.z + spacing));
                    connectivity.Add(anchor);

                    anchor = 0;
                }
            }

            // ground details
            for (int i = 0; i < 600; i++)
            {
                var posA = new float3(random.NextFloat(-55f, 55f), 0f, random.NextFloat(-55f, 55f));
                var posB = posA;

                posA.x += random.NextFloat(-.2f, -.1f);
                posA.y += random.NextFloat(0f, 3f);
                posA.z += random.NextFloat(.1f, .2f);
                points.Add(posA);

                connectivity.Add(0);

                posB.x += random.NextFloat(.2f, .1f);
                posB.y += random.NextFloat(0f, .2f);
                posB.z += random.NextFloat(-.1f, -.2f);
                points.Add(posB);

                if (random.NextFloat() < .1f)
                    connectivity.Add(byte.MaxValue);
                else
                    connectivity.Add(0);
            }

            var pointCount = points.Length;

            void IncreaseConnectivity(int index)
            {
                var value = connectivity[index];
                if (value < byte.MaxValue)
                {
                    connectivity[index] = (byte)(value + 1);
                }
            }

            var bars = new NativeList<Bar>(Allocator.Temp);
            var colors = new NativeList<float4>(Allocator.Temp);

            for (int i = 0; i < points.Length; i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    var delta = points[j] - points[i];
                    var lengthsq = math.lengthsq(delta);
                    if (lengthsq < 5f * 5f && lengthsq > .2f * .2f)
                    {
                        IncreaseConnectivity(i);
                        IncreaseConnectivity(j);

                        var length = math.sqrt(lengthsq);
                        bars.Add(new Bar { pointA = i, pointB = j, length = length });

                        float upDot = math.acos(math.abs(math.dot(new float3(0, 1, 0), delta / length))) / math.PI;
                        colors.Add(new float4(new float3(upDot * random.NextFloat(.7f, 1f)), 1f));
                    }
                }
            }

            var dsParent = new NativeArray<int>(points.Length, Allocator.Temp);
            var dsSize = new NativeArray<int>(points.Length, Allocator.Temp);

            for (int i = 0; i < dsParent.Length; i++)
            {
                dsParent[i] = i;
                dsSize[i] = 1;
            }

            int FindRoot(int i)
            {
                if (dsParent[i] == i) return i;
                dsParent[i] = FindRoot(dsParent[i]);
                return dsParent[i];
            }

            for (int i = 0; i < bars.Length; i++)
            {
                var a = FindRoot(bars[i].pointA);
                var b = FindRoot(bars[i].pointB);

                if (a == b) continue;

                if (dsSize[a] < dsSize[b])
                {
                    (a, b) = (b, a);
                }

                dsParent[b] = a;
                dsSize[a] += dsSize[b];
            }

            var pointData = new PointArrays
            {
                current = new NativeArray<float3>(bars.Length * 2, Allocator.Persistent),
                previous = new NativeArray<float3>(bars.Length * 2, Allocator.Persistent),
                connectivity = new NativeArray<byte>(bars.Length * 2, Allocator.Persistent),
                count = new NativeReference<int>(pointCount, Allocator.Persistent)
            };

            for (int i = 0; i < points.Length; i++)
            {
                pointData.current[i] = points[i];
                pointData.previous[i] = points[i];
                pointData.connectivity[i] = connectivity[i];
            }

            state.EntityManager.Instantiate(config.BarPrefab, bars.Length, Allocator.Temp);
            var query = SystemAPI.QueryBuilder().WithAll<Bar, URPMaterialPropertyBaseColor>().Build();
            query.CopyFromComponentDataArray(bars.AsArray());
            query.CopyFromComponentDataArray(colors.AsArray().Reinterpret<URPMaterialPropertyBaseColor>());

            foreach (var thickness in SystemAPI.Query<RefRW<BarThickness>>())
            {
                thickness.ValueRW.Value = random.NextFloat(.25f, .35f);
            }

            var barEntities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < bars.Length; i++)
            {
                var cluster = new BarCluster { Value = FindRoot(bars[i].pointA) };
                state.EntityManager.AddSharedComponent(barEntities[i], cluster);
            }

            var singletonEntity = SystemAPI.GetSingletonEntity<Config>();
            state.EntityManager.AddComponentData(singletonEntity, pointData);

            state.Enabled = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<PointArrays>())
            {
                var points = SystemAPI.GetSingleton<PointArrays>();
                points.current.Dispose();
                points.previous.Dispose();
                points.connectivity.Dispose();
                points.count.Dispose();
            }
        }
    }

    public struct PointArrays : IComponentData
    {
        public NativeArray<float3> current;
        public NativeArray<float3> previous;
        public NativeArray<byte> connectivity;
        public NativeReference<int> count;
    }

    public struct BarCluster : ISharedComponentData
    {
        public int Value;
    }
}
