using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public interface IReceiveEntity
{
    void SetReceivedEntity(Entity entity);
}

public struct SentEntity : IComponentData { }

public class EntitySender : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject[] EntityReceivers;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SentEntity() { });
        foreach( var EntityReceiver in EntityReceivers)
        {
            var potentialReceivers = EntityReceiver.GetComponents<MonoBehaviour>();
            foreach (var potentialReceiver in potentialReceivers)
            {
                if (potentialReceiver is IReceiveEntity receiver)
                {
                    receiver.SetReceivedEntity(entity);
                }
            }
        }
    }
}
