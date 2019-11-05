using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/GridPath/GridPathMovement")]
[ConverterVersion("joe", 1)]
public class GridPathMovementAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Range(0.0f, 2.0f)]
    public float Speed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new GridDirection
        {
            Value = 0, // default N
        });
        dstManager.AddComponentData(entity, new GridSpeed
        {
            Value = (ushort)(Speed * 1024.0f)
        });
        dstManager.AddComponentData(entity, new GridPosition
        {
            x = -1,
            y = -1
        });
    }
}
