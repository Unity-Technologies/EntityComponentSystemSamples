using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

public struct VisualizedRaycast : IComponentData
{
    public float RayLength;

    public Entity FullRayEntity;
    public Entity HitRayEntity;
    public Entity HitPositionEntity;
}

// An authoring component that configures a visualization for a raycast
[DisallowMultipleComponent]
public class VisualizeRaycastAuthoring : MonoBehaviour
{
    [Tooltip("The length of the desired raycast")]
    public float RayLength;

    [Header("Visualization Elements")]
    [Tooltip("An object that will be scaled along its z-axis to visualize the full length of the ray cast")]
    public Transform FullRay;
    [Tooltip("An object that will be scaled along its z-axis to visualize the distance from the ray start to the hit position of the raycast, if the raycast is successful")]
    public Transform HitRay;
    [Tooltip("An object that will be snapped to the hit position of the raycast, if the raycast is successful")]
    public Transform HitPosition;
}

class VisualizeRaycastBaker : Baker<VisualizeRaycastAuthoring>
{
    public override void Bake(VisualizeRaycastAuthoring authoring)
    {
        var fullRayEntity = GetEntity(authoring.FullRay);
        var hitRayEntity = GetEntity(authoring.HitRay);
        var hitPosEntity = GetEntity(authoring.HitPosition);

        Assert.IsTrue(fullRayEntity != Entity.Null);
        Assert.IsTrue(hitRayEntity != Entity.Null);
        Assert.IsTrue(hitPosEntity != Entity.Null);
        Assert.IsTrue(authoring.RayLength != 0.0f);

        VisualizedRaycast visualizedRaycast = new VisualizedRaycast
        {
            RayLength = authoring.RayLength,

            FullRayEntity = fullRayEntity,
            HitRayEntity = hitRayEntity,
            HitPositionEntity = hitPosEntity,
        };

        AddComponent(visualizedRaycast);
    }
}
