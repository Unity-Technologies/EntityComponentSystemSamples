using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RotateComponent_GO : MonoBehaviour, IConvertGameObjectToEntity
{
    public Vector3 LocalAngularVelocity = Vector3.zero;

    #region ECS
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new RotateComponent_GO_ECS
        { 
            // We can convert to radians/sec once here.
            LocalAngularVelocity = math.radians(LocalAngularVelocity),
        });
        // Rotate System updates the Rotation component,
        // so we add one if one doesn't already exist
        if(!dstManager.HasComponent<Rotation>(entity))
        {
            dstManager.AddComponentData(entity, new Rotation { Value = transform.rotation });
        }
    }
    #endregion
}

#region ECS
public struct RotateComponent_GO_ECS : IComponentData
{
    public float3 LocalAngularVelocity; // in radians/sec
}
#endregion

