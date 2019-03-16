using Unity.Physics;
using Unity.Collections;
using Unity.Entities;
using System;
using UnityEngine;

public struct EntityKiller : IComponentData
{
    public int TimeToDie;
}

public class EntityKillerBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public int TimeToDie;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData<EntityKiller>(entity, new EntityKiller() { TimeToDie = TimeToDie });
    }
}

public class EntityKillerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach(
            (Entity entity, ref EntityKiller killer) =>
            {
                killer.TimeToDie--;
                if(killer.TimeToDie <= 0)
                {
                    PostUpdateCommands.DestroyEntity(entity);
                }
            }
        );
    }
}
