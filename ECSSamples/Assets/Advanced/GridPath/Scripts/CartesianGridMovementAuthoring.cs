using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/GridPath/Cartesian Grid Movement")]
[ConverterVersion("joe", 1)]
public class CartesianGridMovementAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Range(0.0f, 2.0f)]
    public float Speed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CartesianGridDirection
        {
            Value = 0, // default N
        });
        dstManager.AddComponentData(entity, new CartesianGridSpeed
        {
            Value = (ushort)(Speed * 1024.0f)
        });
        dstManager.AddComponentData(entity, new CartesianGridCoordinates
        {
            x = -1,
            y = -1
        });
    }
}
