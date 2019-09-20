using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class EntityTracker : MonoBehaviour, IReceiveEntity
{
    private Entity EntityToTrack = Entity.Null;
    public void SetReceivedEntity(Entity entity)
    {
        EntityToTrack = entity;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (EntityToTrack != Entity.Null)
        {
            try
            {
                var entityManager = BasePhysicsDemo.DefaultWorld.EntityManager;

                transform.position = entityManager.GetComponentData<Translation>(EntityToTrack).Value;
                transform.rotation = entityManager.GetComponentData<Rotation>(EntityToTrack).Value;
            }
            catch
            {
                // Dirty way to check for an Entity that no longer exists.
                EntityToTrack = Entity.Null;
            }
        }
    }
}
