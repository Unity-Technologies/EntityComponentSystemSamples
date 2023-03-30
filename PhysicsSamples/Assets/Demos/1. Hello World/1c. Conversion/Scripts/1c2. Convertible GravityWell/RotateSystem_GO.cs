using Unity.Burst;
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
public partial struct RotateSystem_GO_ECS : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Create local deltaTime so it is accessible inside the ForEach lambda
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var(transform, rotator) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotateComponent_GO_ECS>>())
        {
            var av = rotator.ValueRO.LocalAngularVelocity * deltaTime;
            transform.ValueRW.Rotation = math.mul(transform.ValueRW.Rotation, quaternion.EulerZXY(av));
        }
    }
}
#endregion
