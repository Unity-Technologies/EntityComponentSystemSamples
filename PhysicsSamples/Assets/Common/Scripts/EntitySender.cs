using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public interface IReceiveEntity
{
}

public struct SentEntity : IBufferElementData
{
    public Entity Target;
}

public class EntitySender : MonoBehaviour
{
    public GameObject[] EntityReceivers;

    class EntitySenderBaker : Baker<EntitySender>
    {
        public override void Bake(EntitySender authoring)
        {
            var sentEntities = AddBuffer<SentEntity>();
            foreach (var entityReceiver in authoring.EntityReceivers)
            {
                List<MonoBehaviour> potentialReceivers = new List<MonoBehaviour>();
                GetComponents<MonoBehaviour>(entityReceiver, potentialReceivers);
                foreach (var potentialReceiver in potentialReceivers)
                {
                    if (potentialReceiver is IReceiveEntity)
                    {
                        sentEntities.Add(new SentEntity() {Target = GetEntity(entityReceiver)});
                    }
                }
            }
        }
    }
}
