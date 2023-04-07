using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[AddComponentMenu("DOTS Samples/IJobEntityBatch/Move Speed")]
[ConverterVersion("joe", 1)]
public class MoveSpeedAuthoring_IJobEntityBatch : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MoveSpeed = 1f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new MoveSpeed_IJobEntityBatch { MoveSpeed = new Vector3(0f, 0f, MoveSpeed) };
        dstManager.AddComponentData(entity, data);
    }
}
