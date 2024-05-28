using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class EntitySender : MonoBehaviour
{
    public GameObject[] EntityReceivers;

    class EntitySenderBaker : Baker<EntitySender>
    {
        public override void Bake(EntitySender authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var sentEntities = AddBuffer<SentEntity>(entity);
            foreach (var entityReceiver in authoring.EntityReceivers)
            {
                List<MonoBehaviour> potentialReceivers = new List<MonoBehaviour>();
                GetComponents<MonoBehaviour>(entityReceiver, potentialReceivers);
                foreach (var potentialReceiver in potentialReceivers)
                {
                    if (potentialReceiver is IReceiveEntity)
                    {
                        sentEntities.Add(new SentEntity()
                        {
                            Target = GetEntity(entityReceiver, TransformUsageFlags.Dynamic)
                        });
                    }
                }
            }
        }
    }
}

public interface IReceiveEntity
{
}

public struct SentEntity : IBufferElementData
{
    public Entity Target;
}
