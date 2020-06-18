using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RotateComponentAuthoring_DOTS : MonoBehaviour, IConvertGameObjectToEntity
{
    public Vector3 LocalAngularVelocity = Vector3.zero; // in degrees/sec

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new RotateComponent_DOTS
        {
            // We can convert to radians/sec once here.
            LocalAngularVelocity = math.radians(LocalAngularVelocity),
        });
    }
}

