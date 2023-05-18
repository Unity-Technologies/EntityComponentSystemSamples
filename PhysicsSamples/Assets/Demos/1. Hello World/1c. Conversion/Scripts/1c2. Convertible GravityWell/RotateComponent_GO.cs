using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RotateComponent_GO : MonoBehaviour
{
    public Vector3 LocalAngularVelocity = Vector3.zero;

    // Added Baker, but is disabled as this is in a redundant sample with ConvertToEntity gone
    /*
    #region ECS
    class Baker : Baker<RotateComponent_GO>
    {
        public override void Bake(RotateComponent_GO authoring)
        {
            AddComponent(new RotateComponent_GO_ECS
            {
                // We can convert to radians/sec once here.
                LocalAngularVelocity = math.radians(authoring.LocalAngularVelocity),
            });
            // Rotate System updates the LocalTransform component,
            // so we add one if one doesn't already exist
            if (!dstManager.HasComponent<LocalTransform>(entity))
            {
                dstManager.AddComponentData(entity, new LocalTransform { Value = TransformData.FromRotation(transform.rotation) });
            }
        }
    }
    #endregion
    */
}

#region ECS
public struct RotateComponent_GO_ECS : IComponentData
{
    public float3 LocalAngularVelocity; // in radians/sec
}
#endregion
