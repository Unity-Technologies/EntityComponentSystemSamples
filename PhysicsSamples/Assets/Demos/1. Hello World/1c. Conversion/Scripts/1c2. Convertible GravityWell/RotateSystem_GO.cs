using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RotateSystem_GO : MonoBehaviour
{
    void Update()
    {
        foreach (var rotator in FindObjectsOfType<RotateComponent_GO>())
        {
            var av = rotator.LocalAngularVelocity * Time.deltaTime;
            var rotation = Quaternion.Euler(av);
            rotator.gameObject.transform.localRotation *= rotation;
        }
    }
}


#region ECS
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(GravityWellSystem_GO_ECS))]
public partial class RotateSystem_GO_ECS : SystemBase
{
    protected override void OnUpdate()
    {
        // Create local deltaTime so it is accessible inside the ForEach lambda
        var deltaTime = SystemAPI.Time.DeltaTime;

        Entities
            .WithBurst()
#if !ENABLE_TRANSFORM_V1
            .ForEach((ref LocalTransform transform, in RotateComponent_GO_ECS rotator) =>
            {
                var av = rotator.LocalAngularVelocity * deltaTime;
                transform.Rotation = math.mul(transform.Rotation, quaternion.EulerZXY(av));
            }).Run();
#else
            .ForEach((ref Rotation rotation, in RotateComponent_GO_ECS rotator) =>
            {
                var av = rotator.LocalAngularVelocity * deltaTime;
                rotation.Value = math.mul(rotation.Value, quaternion.EulerZXY(av));
            }).Run();
#endif
    }
}
#endregion
