using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/GridPath/Cartesian Grid Movement")]
[ConverterVersion("macton", 4)]
public class CartesianGridMovementAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public enum MovementOptions
    {
        Bounce,
        FollowTarget
    };

    [Range(0.0f, 2.0f)]
    public float Speed;
    public MovementOptions Movement;

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
            x = 0,
            y = 0 
        });

        if (Movement == MovementOptions.FollowTarget)
            dstManager.AddComponentData(entity, new CartesianGridFollowTarget());
    }
}
