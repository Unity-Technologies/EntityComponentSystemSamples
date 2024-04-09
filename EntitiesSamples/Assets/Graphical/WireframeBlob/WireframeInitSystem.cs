using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Graphical.PrefabInitializer
{
    [BurstCompile]
    public partial struct WireframeInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteWireframeBlob>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var uninitializedQuery = SystemAPI.QueryBuilder()
                .WithAll<WireframeLocalSpace>()
                .WithNone<WireframeWorldSpace>().Build();
            var entities = uninitializedQuery.ToEntityArray(Allocator.Temp);

            state.EntityManager.AddComponent<WireframeWorldSpace>(uninitializedQuery);

            var worldSpaceShellLookup = SystemAPI.GetBufferLookup<WireframeWorldSpace>();
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
            var postTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true);

            foreach (var entity in entities)
            {
                var localSpace = SystemAPI.GetComponent<WireframeLocalSpace>(entity);
                ref var input = ref localSpace.Blob.Value.Vertices;
                var worldSpace = worldSpaceShellLookup[entity];
                worldSpace.Resize(input.Length, NativeArrayOptions.UninitializedMemory);
                var output = worldSpace.Reinterpret<float3>();

                TransformHelpers.ComputeWorldTransformMatrix(entity, out var matrix, ref localTransformLookup,
                    ref parentLookup, ref postTransformMatrixLookup);

                for (int i = 0; i < input.Length; i++)
                {
                    output[i] = math.mul(matrix, new float4(input[i], 1)).xyz;
                }
            }
        }
    }
}
