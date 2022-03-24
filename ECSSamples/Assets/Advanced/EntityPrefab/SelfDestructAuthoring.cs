using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
public class SelfDestructAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float TimeToLive;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SelfDestruct {TimeToLive = TimeToLive});
    }
}

#endif
