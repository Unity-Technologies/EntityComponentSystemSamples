using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;

class InitialVelocityAuthoring : MonoBehaviour
{
    public Vector3 Linear;
    public Vector3 Angular;

    class InitialVelocityAuthoringBaker : Baker<InitialVelocityAuthoring>
    {
        public override void Bake(InitialVelocityAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new InitialVelocity { Linear = authoring.Linear, Angular = authoring.Angular });
        }
    }
}

public struct InitialVelocity : IComponentData
{
    public float3 Linear;
    public float3 Angular;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ApplyInitialVelocity : ISystem, ISystemStartStop
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InitialVelocity>();
        state.RequireForUpdate<PhysicsVelocity>();
    }

    public void OnStartRunning(ref SystemState state)
    {
        foreach (var(initialVel, velocity) in SystemAPI.Query<RefRO<InitialVelocity>, RefRW<PhysicsVelocity>>())
        {
            velocity.ValueRW.Linear = initialVel.ValueRO.Linear;
            velocity.ValueRW.Angular = initialVel.ValueRO.Angular;
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
    }
}
