using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/GridPath/Cartesian Grid Target")]
[ConverterVersion("macton", 1)]
public class CartesianGridTargetAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<CartesianGridTarget>(entity);
        dstManager.AddComponentData(entity, new CartesianGridTargetCoordinates { x = -1, y = -1 });
    }
}
