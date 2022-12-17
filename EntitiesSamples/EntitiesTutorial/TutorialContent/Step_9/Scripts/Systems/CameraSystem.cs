using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
// This system should run after the transform system has been updated, otherwise the camera
// will lag one frame behind the tank and will jitter.
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct CameraSystem : ISystem
{
    Entity Target;
    Random Random;
    EntityQuery TanksQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        Random = Random.CreateFromIndex(1234);
        TanksQuery = SystemAPI.QueryBuilder().WithAll<Tank>().Build(); 
        state.RequireForUpdate(TanksQuery);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
    
    // Because this OnUpdate accesses managed objects, it cannot be Burst-compiled.
    public void OnUpdate(ref SystemState state)
    {
        if (Target == Entity.Null || UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Space))
        {
            var tanks = TanksQuery.ToEntityArray(Allocator.Temp);
            Target = tanks[Random.NextInt(tanks.Length)];
        }

        var cameraTransform = CameraSingleton.Instance.transform;
        var tankTransform = SystemAPI.GetComponent<LocalToWorld>(Target);
        cameraTransform.position = tankTransform.Position - 10.0f * tankTransform.Forward + new float3(0.0f, 5.0f, 0.0f);
        cameraTransform.LookAt(tankTransform.Position, new float3(0.0f, 1.0f, 0.0f));
    }
}