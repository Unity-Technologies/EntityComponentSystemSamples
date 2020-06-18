using Unity.Entities;
using UnityEngine;

public class GravityWellComponentAuthoring_DOTS : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Strength = 100.0f;
    public float Radius = 10.0f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new GravityWellComponent_DOTS
        {
            Strength = Strength,
            Radius = Radius,
            Position = gameObject.transform.position
        });
    }
}