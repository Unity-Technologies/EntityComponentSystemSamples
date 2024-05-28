using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public struct DriveClientAsDynamic : IComponentData {}

public class DriveClientAsDynamicAuthoring : MonoBehaviour {}

public class DriveClientAsDynamicBaker : Baker<DriveClientAsDynamicAuthoring>
{
    public override void Bake(DriveClientAsDynamicAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new DriveClientAsDynamic {});
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct DriveClientAsDynamicSystem : ISystem
{
    internal ComponentLookup<DriveClientAsDynamic> m_DriveClientAsDynamic;
    internal ComponentLookup<PhysicsCollider> m_PhysicsCollider;
    internal ComponentLookup<PhysicsGravityFactor> m_GravityFactor;
    internal ComponentLookup<PhysicsMass> m_PhysicsMass;

    public void OnCreate(ref SystemState state)
    {
        m_DriveClientAsDynamic = state.GetComponentLookup<DriveClientAsDynamic>(true);
        m_PhysicsCollider = state.GetComponentLookup<PhysicsCollider>(true);
        m_GravityFactor = state.GetComponentLookup<PhysicsGravityFactor>(false);
        m_PhysicsMass = state.GetComponentLookup<PhysicsMass>(true);
        state.RequireForUpdate<DriveClientAsDynamic>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var commandBufferSingleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = commandBufferSingleton.CreateCommandBuffer(state.World.Unmanaged);
        m_DriveClientAsDynamic.Update(ref state);
        m_PhysicsCollider.Update(ref state);
        m_GravityFactor.Update(ref state);
        m_PhysicsMass.Update(ref state);

        foreach (var(proxyDriver, client) in SystemAPI.Query<RefRO<CustomPhysicsProxyDriver>>().WithEntityAccess())
        {
            if (m_DriveClientAsDynamic.TryGetComponent(proxyDriver.ValueRO.rootEntity, out DriveClientAsDynamic driveClientAsDynamic))
            {
                commandBuffer.RemoveComponent<DriveClientAsDynamic>(proxyDriver.ValueRO.rootEntity);
                commandBuffer.AddComponent(client, new Simulate());

                var serverMass = m_PhysicsMass[proxyDriver.ValueRO.rootEntity];
                if (serverMass.IsKinematic)
                {
                    // set something big if we have a kinematic body on server, otherwise take the server mass as it is
                    serverMass.InverseMass = 1.0f / 10000f;
                }

                commandBuffer.SetComponent(client, serverMass);
                if (m_GravityFactor.HasComponent(client))
                {
                    m_GravityFactor[client] = new PhysicsGravityFactor { Value = 0.0f }; // disable gravity
                }
                else
                {
                    commandBuffer.AddComponent(client, new PhysicsGravityFactor { Value = 0.0f });
                }
            }
        }
    }
}
