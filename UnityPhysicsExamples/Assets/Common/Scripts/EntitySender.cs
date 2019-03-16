using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


public interface IRecieveEntity
{
    void SetRecievedEntity(Entity entity);
}

public struct SentEntity : IComponentData { }

public class EntitySender : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject[] EntityRecievers;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SentEntity() { });
        foreach( var EntityReciever in EntityRecievers)
        {
            var potentialRecievers = EntityReciever.GetComponents<MonoBehaviour>();
            foreach (var potentialReciever in potentialRecievers)
            {
                if (potentialReciever is IRecieveEntity)
                {
                    (potentialReciever as IRecieveEntity).SetRecievedEntity(entity);
                }
            }
        }
    }
}
