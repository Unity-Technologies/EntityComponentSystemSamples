using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Stateful;
using Unity.Transforms;
using UnityEngine;

public struct DisplayCollisionTextComponent : IComponentData
{
    public int CollisionDurationCount;
    public int FramesSinceCollisionExit;
}

public class DisplayCollisionText : MonoBehaviour, IReceiveEntity
{
    class DisplayCollisionTextBaker : Baker<DisplayCollisionText>
    {
        public override void Bake(DisplayCollisionText authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DisplayCollisionTextComponent()
            {
                CollisionDurationCount = 0,
                FramesSinceCollisionExit = 0
            });
        }
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct DisplayCollisionTextSystem : ISystem
{
    EntityQuery _TextMeshQuery;
    EntityQuery _SentEntitiesQuery;

    private const int k_FramesToStay = 20;

    private float3 k_TextOffset;

    struct CollisionEntities
    {
        public Entity TextEntity;
        public Entity DisplayEntity;
        public Entity CollisionEventEntity;
    }

    public void OnCreate(ref SystemState state)
    {
        k_TextOffset = new float3(0, 1.5f, 0);
        _TextMeshQuery = state.GetEntityQuery(typeof(DisplayCollisionTextComponent), typeof(TextMesh));
        _SentEntitiesQuery = state.GetEntityQuery(typeof(SentEntity));
    }

    public void OnUpdate(ref SystemState state)
    {
        var textEntities = _TextMeshQuery.ToEntityArray(Allocator.Temp);
        var sentEntities = _SentEntitiesQuery.ToEntityArray(Allocator.Temp);

        NativeArray<CollisionEntities> eventArray = new NativeArray<CollisionEntities>(textEntities.Length, Allocator.Temp);

        GetEntityMapping(state.EntityManager, eventArray, textEntities, sentEntities);

        for (int idx = 0; idx < eventArray.Length; ++idx)
        {
            var mapping = eventArray[idx];

            if (!state.EntityManager.Exists(mapping.DisplayEntity) ||
                !state.EntityManager.Exists(mapping.CollisionEventEntity))
            {
                continue;
            }

            var displayTransform = state.EntityManager.GetComponentData<LocalTransform>(mapping.DisplayEntity);
            var buffer = state.EntityManager.GetBuffer<StatefulCollisionEvent>(mapping.CollisionEventEntity);

            var displayCollisionTextComponent = state.EntityManager.GetComponentData<DisplayCollisionTextComponent>(mapping.TextEntity);
            var textMesh = state.EntityManager.GetComponentObject<TextMesh>(mapping.TextEntity);

            for (int i = 0; i < buffer.Length; i++)
            {
                var collisionEvent = buffer[i];
                if (collisionEvent.GetOtherEntity(mapping.CollisionEventEntity) != mapping.DisplayEntity)
                {
                    continue;
                }

                switch (collisionEvent.State)
                {
                    case StatefulEventState.Enter:
                        displayCollisionTextComponent.CollisionDurationCount = 0;
                        displayCollisionTextComponent.FramesSinceCollisionExit = 0;
                        textMesh.text = "Collision Enter";
                        textMesh.color = Color.red;
                        break;
                    case StatefulEventState.Stay:
                        textMesh.text = "";
                        if (displayCollisionTextComponent.CollisionDurationCount++ < k_FramesToStay)
                        {
                            textMesh.text = "Collision Enter " + displayCollisionTextComponent.CollisionDurationCount + " frames ago.\n";
                        }
                        else
                        {
                            textMesh.color = Color.white;
                        }
                        textMesh.text += "Collision Stay " + displayCollisionTextComponent.CollisionDurationCount + " frames.";
                        break;
                    case StatefulEventState.Exit:
                        displayCollisionTextComponent.FramesSinceCollisionExit++;
                        textMesh.text = "Collision lasted " + displayCollisionTextComponent.CollisionDurationCount + " frames.";
                        textMesh.color = Color.yellow;
                        break;
                }
            }

            if (displayCollisionTextComponent.FramesSinceCollisionExit != 0)
            {
                if (displayCollisionTextComponent.FramesSinceCollisionExit++ == k_FramesToStay)
                {
                    displayCollisionTextComponent.FramesSinceCollisionExit = 0;
                    textMesh.text = "";
                }
            }

            state.EntityManager.SetComponentData(mapping.TextEntity, displayCollisionTextComponent);

            var textTransform = SystemAPI.GetComponentRW<LocalTransform>(mapping.TextEntity, false);
            textTransform.ValueRW.Position = displayTransform.Position + k_TextOffset;
        }
    }

    [BurstCompile]
    static void GetEntityMapping(EntityManager entityManager, NativeArray<CollisionEntities> eventArray, NativeArray<Entity> textEntities, NativeArray<Entity> sentEntities)
    {
        for (int i = 0; i < eventArray.Length; ++i)
        {
            CollisionEntities ce = new CollisionEntities();
            ce.TextEntity = textEntities[i];

            for (int sourceIdx = 0, count = sentEntities.Length; sourceIdx < count; ++sourceIdx)
            {
                var sentEntity = sentEntities[sourceIdx];
                var targetBuffer = entityManager.GetBuffer<SentEntity>(sentEntity);
                for (int targetIdx = 0; targetIdx < targetBuffer.Length; ++targetIdx)
                {
                    if (targetBuffer[targetIdx].Target == textEntities[i])
                    {
                        if (entityManager.HasComponent<StatefulCollisionEvent>(sentEntity))
                            ce.CollisionEventEntity = sentEntity;
                        else
                            ce.DisplayEntity = sentEntity;
                    }
                }
            }

            eventArray[i] = ce;
        }
    }
}
