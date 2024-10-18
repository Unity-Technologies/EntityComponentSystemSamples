using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public struct PhysicsPyramid : IComponentData {}

public struct CreatePyramids : IComponentData
{
    public Entity BoxEntity;
    public int Count;
    public int Height;
    public int Space;
    public float3 StartPosition;
    public float3 BoxSize;
}


public class CreatePyramidsBehaviour : MonoBehaviour
{
    public GameObject boxPrefab;
    public int Count = 5;
    public int Height = 5;
    public int Space = 2;

    class CreatePyramidsBaker : Baker<CreatePyramidsBehaviour>
    {
        public override void Bake(CreatePyramidsBehaviour authoring)
        {
            var sourceEntity = GetEntity(authoring.boxPrefab, TransformUsageFlags.Dynamic);
            if (sourceEntity == Entity.Null)
                return;

            var boxSize = float3.zero;
            var renderer = authoring.boxPrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                boxSize = renderer.bounds.size;
            }

            var createPyramids = new CreatePyramids
            {
                BoxEntity = sourceEntity,
                Count = authoring.Count,
                Height = authoring.Height,
                Space = authoring.Space,
                StartPosition = authoring.transform.position,
                BoxSize = boxSize
            };
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, createPyramids);
        }
    }
}


[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class CreatePyramidsSystem : SystemBase
{
    private EntityCommandBufferSystem ECBSystem;
    protected override void OnCreate()
    {
        ECBSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
        RequireForUpdate<CreatePyramids>();
    }

    protected override void OnUpdate()
    {
        var ecb = ECBSystem.CreateCommandBuffer();
        foreach (var(creator, creatorEntity) in SystemAPI.Query<RefRO<CreatePyramids>>().WithEntityAccess())
        {
            float3 boxSize = creator.ValueRO.BoxSize;
            int boxCount = creator.ValueRO.Count * (creator.ValueRO.Height * (creator.ValueRO.Height + 1) / 2);

            var positions = new NativeArray<float3>(boxCount, Allocator.Temp);

            int boxIdx = 0;
            for (int p = 0; p < creator.ValueRO.Count; p++)
            {
                for (int i = 0; i < creator.ValueRO.Height; i++)
                {
                    int rowSize = creator.ValueRO.Height - i;
                    float3 start = new float3(-rowSize * boxSize.x * 0.5f + boxSize.x * 0.5f, i * boxSize.y, 0);
                    for (int j = 0; j < rowSize; j++)
                    {
                        float3 shift = new float3(j * boxSize.x, 0f, p * boxSize.z * creator.ValueRO.Space);
                        positions[boxIdx] = creator.ValueRO.StartPosition;
                        positions[boxIdx] += start + shift;
                        boxIdx++;
                    }
                }
            }

            var pyramidComponent = new PhysicsPyramid();
            for (int i = 0; i < positions.Length; i++)
            {
                var entity = EntityManager.Instantiate(creator.ValueRO.BoxEntity);
                ecb.AddComponent(entity, pyramidComponent);

                var transform = EntityManager.GetComponentData<LocalTransform>(entity);
                transform.Position = positions[i];
                ecb.SetComponent(entity, transform);
            }

            ecb.DestroyEntity(creatorEntity);
        }
    }
}
