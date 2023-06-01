using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Common.Scripts
{
    public class EntitySender : MonoBehaviour
    {
        [FormerlySerializedAs("EntityReceivers")]
        public GameObject[] Receivers;

        class Baker : Baker<EntitySender>
        {
            public override void Bake(EntitySender authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var sentEntities = AddBuffer<TargetEntity>(entity);
                foreach (var receiver in authoring.Receivers)
                {
                    List<MonoBehaviour> mbs = new List<MonoBehaviour>();
                    GetComponents<MonoBehaviour>(receiver, mbs);
                    foreach (var mb in mbs)
                    {
                        if (mb is IReceiveEntity)
                        {
                            sentEntities.Add(new TargetEntity()
                            {
                                Value = GetEntity(mb, TransformUsageFlags.Dynamic)
                            });
                        }
                    }
                }
            }
        }
    }

    public struct TargetEntity : IBufferElementData
    {
        public Entity Value;
    }

    public interface IReceiveEntity
    {
    }
}
