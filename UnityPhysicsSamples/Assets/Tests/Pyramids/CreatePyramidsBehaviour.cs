using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;

public struct PhysicsPyramid : IComponentData { }

public struct CreatePyramids : IComponentData
{
    public Entity BoxEntity;
    public int Count;
    public int Height;
    public int Space;
    public float3 StartPosition;
    public float3 BoxSize;
}


public class CreatePyramidsBehaviour : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject boxPrefab;
    public int Count = 5;
    public int Height = 5;
    public int Space = 2;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(boxPrefab);
    }

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var sourceEntity = conversionSystem.GetPrimaryEntity(boxPrefab);
        if (sourceEntity == null)
            return;

        var boxSize = float3.zero;
        var renderer = boxPrefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            boxSize = renderer.bounds.size;
        }

        var createPyramids = new CreatePyramids
        {
            BoxEntity = conversionSystem.GetPrimaryEntity(boxPrefab),
            Count = Count,
            Height = Height,
            Space = Space,
            StartPosition = transform.position,
            BoxSize = boxSize
        };
        dstManager.AddComponentData<CreatePyramids>(entity, createPyramids);
    }
}


[UpdateBefore(typeof(BuildPhysicsWorld))]
public class CreatePyramidsSystem : ComponentSystem
{
    EntityQuery m_MainGroup;

    protected override void OnCreate()
    {
        m_MainGroup = GetEntityQuery(ComponentType.ReadOnly<CreatePyramids>());
    }

    protected override void OnUpdate()
    {
        var groupEntities = m_MainGroup.ToEntityArray(Allocator.TempJob);
        foreach (var entity in groupEntities)
        {
            var creator = EntityManager.GetComponentData<CreatePyramids>(entity);

            float3 boxSize = creator.BoxSize;
            int boxCount = creator.Count * (creator.Height * (creator.Height + 1) / 2);

            var positions = new NativeArray<float3>(boxCount, Allocator.Temp);
            int boxIdx = 0;
            for (int p = 0; p < creator.Count; p++)
            {
                for (int i = 0; i < creator.Height; i++)
                {
                    int rowSize = creator.Height - i;
                    float3 start = new float3(-rowSize * boxSize.x * 0.5f + boxSize.x * 0.5f, i * boxSize.y, 0);
                    for (int j = 0; j < rowSize; j++)
                    {
                        float3 shift = new float3(j * boxSize.x, 0f, p * boxSize.z * creator.Space);
                        positions[boxIdx] = creator.StartPosition;
                        positions[boxIdx] += start + shift;
                        boxIdx++;
                    }
                }
            }

            var entities = new NativeArray<Entity>(boxCount, Allocator.Temp);
            EntityManager.Instantiate(creator.BoxEntity, entities);

            var pyramidComponent = new PhysicsPyramid();
            for (boxIdx = 0; boxIdx < entities.Length; boxIdx++)
            {
                EntityManager.AddComponentData<PhysicsPyramid>(entities[boxIdx], pyramidComponent);
                EntityManager.SetComponentData<Translation>(entities[boxIdx], new Translation() { Value = positions[boxIdx] });
            }

            entities.Dispose();
            positions.Dispose();

            PostUpdateCommands.DestroyEntity(entity);
        }
        groupEntities.Dispose();
    }
}
