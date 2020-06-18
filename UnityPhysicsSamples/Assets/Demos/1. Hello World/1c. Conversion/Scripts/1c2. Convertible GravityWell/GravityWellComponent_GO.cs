using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GravityWellComponent_GO : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Strength = 100.0f;
    public float Radius = 10.0f;

    #region ECS
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new GravityWellComponent_GO_ECS
        {
            Strength = Strength,
            Radius = Radius,
            Position = gameObject.transform.position
        });
    }
    #endregion
}

#region ECS
public struct GravityWellComponent_GO_ECS : IComponentData
{
    public float Strength;
    public float Radius;
    // Include position of gravity well so all data accessible in one location
    public float3 Position;
}
#endregion

