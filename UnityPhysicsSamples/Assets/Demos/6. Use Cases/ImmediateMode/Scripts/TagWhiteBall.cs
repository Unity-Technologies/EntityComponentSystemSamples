using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct WhiteBall : IComponentData { }

public class TagWhiteBall : MonoBehaviour, IConvertGameObjectToEntity
{
    public ProjectIntoFutureOnCue CueAction = null;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
#if UNITY_EDITOR
        dstManager.SetName(entity, "WhiteBall");
#endif
        dstManager.AddComponentData(entity, new WhiteBall() {});
        if(CueAction != null)
        {
            CueAction.WhiteBallEntity = entity;
        }
    }
}
