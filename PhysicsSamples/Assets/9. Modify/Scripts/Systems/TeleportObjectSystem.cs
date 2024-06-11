using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct TeleportObjectSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TeleportObject>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new TeleportObjectJob().Schedule(state.Dependency);
    }

    public void OnDestroy(ref SystemState state) {}

    // A job that teleports the falling spheres back to the StartingPosition when they reach the EndingPosition.
    // The linear velocity is reset to zero so that the spheres don't fall too fast
    [BurstCompile]
    private partial struct TeleportObjectJob : IJobEntity
    {
        private void Execute(ref LocalTransform localTransform, ref TeleportObject teleport, ref PhysicsVelocity velocity)
        {
            if (localTransform.Position.y < teleport.EndingPosition.y)
            {
                localTransform.Position = teleport.StartingPosition + new float3(0, 7, 0);
                velocity.Linear = float3.zero;
                velocity.Angular = float3.zero;
            }
        }
    }
}
