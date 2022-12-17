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
            AddComponent(new DisplayCollisionTextComponent()
            {
                CollisionDurationCount = 0,
                FramesSinceCollisionExit = 0
            });
        }
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class DisplayCollisionTextSystem : SystemBase
{
    EntityQuery _TextMeshQuery;
    EntityQuery _SentEntitiesQuery;

    private const int k_FramesToStay = 20;

    private readonly float3 k_TextOffset = new float3(0, 1.5f, 0);

    struct CollisionEntities
    {
        public Entity TextEntity;
        public Entity DisplayEntity;
        public Entity CollisionEventEntity;
    }

    protected override void OnCreate()
    {
        base.OnCreate();

        _TextMeshQuery = GetEntityQuery(typeof(DisplayCollisionTextComponent), typeof(TextMesh));
        _SentEntitiesQuery = GetEntityQuery(typeof(SentEntity));
    }

    protected override void OnUpdate()
    {
        var textEntities = _TextMeshQuery.ToEntityArray(Allocator.Temp);
        var sentEntities = _SentEntitiesQuery.ToEntityArray(Allocator.Temp);

        NativeArray<CollisionEntities> eventArray = new NativeArray<CollisionEntities>(textEntities.Length, Allocator.Temp);

        GetEntityMapping(EntityManager, eventArray, textEntities, sentEntities);

        for (int idx = 0; idx < eventArray.Length; ++idx)
        {
            var mapping = eventArray[idx];

            if (!EntityManager.Exists(mapping.DisplayEntity) ||
                !EntityManager.Exists(mapping.CollisionEventEntity))
            {
                continue;
            }

            var displayTransform = EntityManager.GetAspect<TransformAspect>(mapping.DisplayEntity);
            var buffer = EntityManager.GetBuffer<StatefulCollisionEvent>(mapping.CollisionEventEntity);

            var displayCollisionTextComponent = EntityManager.GetComponentData<DisplayCollisionTextComponent>(mapping.TextEntity);
            var textMesh = EntityManager.GetComponentObject<TextMesh>(mapping.TextEntity);
            var textTransform = EntityManager.GetAspect<TransformAspect>(mapping.TextEntity);

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

            EntityManager.SetComponentData(mapping.TextEntity, displayCollisionTextComponent);

            textTransform.LocalPosition = displayTransform.LocalPosition + k_TextOffset;
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
