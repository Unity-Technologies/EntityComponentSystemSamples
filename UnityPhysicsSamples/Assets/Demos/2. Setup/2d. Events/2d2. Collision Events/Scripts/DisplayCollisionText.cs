using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Stateful;
using Unity.Transforms;
using UnityEngine;

public class DisplayCollisionText : MonoBehaviour, IReceiveEntity
{
    private Entity m_DisplayEntity;
    private Entity m_CollisionEventEntity;

    private int m_CollisionDurationCount;
    private int m_FramesSinceCollisionExit;

    private const int k_FramesToStay = 20;

    private readonly float3 k_TextOffset = new float3(0, 1.5f, 0);

    public void SetReceivedEntity(Entity entity)
    {
        if (World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<StatefulCollisionEvent>(entity))
        {
            m_CollisionEventEntity = entity;
        }
        else
        {
            m_DisplayEntity = entity;
        }
    }

    void Update()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated ||
            !World.DefaultGameObjectInjectionWorld.EntityManager.Exists(m_DisplayEntity) ||
            !World.DefaultGameObjectInjectionWorld.EntityManager.Exists(m_CollisionEventEntity))
        {
            return;
        }

        var pos = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Translation>(m_DisplayEntity);
        var buffer = World.DefaultGameObjectInjectionWorld.EntityManager.GetBuffer<StatefulCollisionEvent>(m_CollisionEventEntity);

        var transform = GetComponent<Transform>();
        var textMesh = GetComponent<TextMesh>();

        for (int i = 0; i < buffer.Length; i++)
        {
            var collisionEvent = buffer[i];
            if (collisionEvent.GetOtherEntity(m_CollisionEventEntity) != m_DisplayEntity)
            {
                continue;
            }

            switch (collisionEvent.CollidingState)
            {
                case EventCollidingState.Enter:
                    m_CollisionDurationCount = 0;
                    m_FramesSinceCollisionExit = 0;
                    textMesh.text = "Collision Enter";
                    textMesh.color = Color.red;
                    break;
                case EventCollidingState.Stay:
                    textMesh.text = "";
                    if (m_CollisionDurationCount++ < k_FramesToStay)
                    {
                        textMesh.text = "Collision Enter " + m_CollisionDurationCount + " frames ago.\n";
                    }
                    else
                    {
                        textMesh.color = Color.white;
                    }
                    textMesh.text += "Collision Stay " + m_CollisionDurationCount + " frames.";
                    break;
                case EventCollidingState.Exit:
                    m_FramesSinceCollisionExit++;
                    textMesh.text = "Collision lasted " + m_CollisionDurationCount + " frames.";
                    textMesh.color = Color.yellow;
                    break;
            }
        }

        if (m_FramesSinceCollisionExit != 0)
        {
            if (m_FramesSinceCollisionExit++ == k_FramesToStay)
            {
                m_FramesSinceCollisionExit = 0;
                textMesh.text = "";
            }
        }

        transform.position = pos.Value + k_TextOffset;
    }
}
