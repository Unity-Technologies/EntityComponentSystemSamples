using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Graphical.Splines
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct SplineBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteSplines>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bakingOnlyEntities = new NativeList<Entity>(state.WorldUpdateAllocator);

            foreach (var (spline, splineBakingData, bufferEntity, bufferPosition) in
                     SystemAPI.Query<RefRW<Spline>, RefRO<SplineBakingData>, DynamicBuffer<SplineControlPointEntity>,
                             DynamicBuffer<SplineControlPointPosition>>())
            {
                bakingOnlyEntities.AddRange(bufferEntity.Reinterpret<Entity>().AsNativeArray());

                using var bakedPoints = BakePoints(state.WorldUpdateAllocator,
                    bufferPosition.Reinterpret<float3>().AsNativeArray(),
                    splineBakingData.ValueRO.Subdivisions);
                using var blobBuilder = new BlobBuilder(state.WorldUpdateAllocator);

                ref var splineData = ref blobBuilder.ConstructRoot<SplineData>();

                var pointsBuilder = blobBuilder.Allocate(ref splineData.Points, bakedPoints.Length);
                var distanceBuilder = blobBuilder.Allocate(ref splineData.Distance, bakedPoints.Length + 1);

                var previous = bakedPoints[0];
                var distance = 0f;

                for (int i = 0; i < bakedPoints.Length; i += 1)
                {
                    pointsBuilder[i] = bakedPoints[i];
                    distance += math.distance(previous, bakedPoints[i]);
                    distanceBuilder[i] = distance;
                    previous = bakedPoints[i];
                }

                distance += math.distance(previous, bakedPoints[0]);
                distanceBuilder[bakedPoints.Length] = distance;

                spline.ValueRW.Data = blobBuilder.CreateBlobAssetReference<SplineData>(Allocator.Persistent);
            }

            state.EntityManager.AddComponent<BakingOnlyEntity>(bakingOnlyEntities.AsArray());
        }

        NativeList<float3> BakePoints(Allocator allocator, NativeArray<float3> controlPoints, int subdivisions)
        {
            float3 RingBufferAt(int index)
            {
                index %= controlPoints.Length;
                if (index < 0) index += controlPoints.Length;
                return controlPoints[index];
            }

            var baked = new NativeList<float3>(allocator);
            baked.Add(controlPoints[0]);

            for (int i = 0; i < controlPoints.Length; i += 1)
            {
                float3 p0 = RingBufferAt(i - 1);
                float3 p1 = RingBufferAt(i);
                float3 p2 = RingBufferAt(i + 1);
                float3 p3 = RingBufferAt(i + 2);

                float t0 = 0;
                float t1 = t0 + math.distance(p0, p1);
                float t2 = t1 + math.distance(p1, p2);
                float t3 = t2 + math.distance(p2, p3);

                for (int j = 0; j < subdivisions; j += 1)
                {
                    float t = math.lerp(t1, t2, (1 + j) / (float)subdivisions);

                    float3 a1 = ((t1 - t) * p0 + (t - t0) * p1) / (t1 - t0);
                    float3 a2 = ((t2 - t) * p1 + (t - t1) * p2) / (t2 - t1);
                    float3 a3 = ((t3 - t) * p2 + (t - t2) * p3) / (t3 - t2);

                    float3 b1 = ((t2 - t) * a1 + (t - t0) * a2) / (t2 - t0);
                    float3 b2 = ((t3 - t) * a2 + (t - t1) * a3) / (t3 - t1);

                    float3 c = ((t2 - t) * b1 + (t - t1) * b2) / (t2 - t1);

                    baked.Add(c);
                }
            }

            return baked;
        }
    }
}
