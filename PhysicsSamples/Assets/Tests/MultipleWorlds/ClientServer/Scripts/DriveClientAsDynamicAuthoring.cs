using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public struct DriveClientAsDynamic : IComponentData {}

public class DriveClientAsDynamicAuthoring : MonoBehaviour {}

public class DriveClientAsDynamicBaker : Baker<DriveClientAsDynamicAuthoring>
{
    public override void Bake(DriveClientAsDynamicAuthoring authoring)
    {
        AddComponent(new DriveClientAsDynamic {});
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class DriveClientAsDynamicSystem : SystemBase
{
    internal ComponentLookup<DriveClientAsDynamic> m_DriveClientAsDynamic;
    internal ComponentLookup<PhysicsCollider> m_PhysicsCollider;
    internal ComponentLookup<PhysicsGravityFactor> m_GravityFactor;
    internal ComponentLookup<PhysicsMass> m_PhysicsMass;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_DriveClientAsDynamic = GetComponentLookup<DriveClientAsDynamic>(true);
        m_PhysicsCollider = GetComponentLookup<PhysicsCollider>(true);
        m_GravityFactor = GetComponentLookup<PhysicsGravityFactor>(false);
        m_PhysicsMass = GetComponentLookup<PhysicsMass>(true);
        RequireForUpdate<DriveClientAsDynamic>();
    }

    protected override void OnUpdate()
    {
        var commandBufferSingleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = commandBufferSingleton.CreateCommandBuffer(World.Unmanaged);
        m_DriveClientAsDynamic.Update(this);
        m_PhysicsCollider.Update(this);
        m_GravityFactor.Update(this);
        m_PhysicsMass.Update(this);

        foreach (var(proxyDriver, client) in SystemAPI.Query<RefRO<CustomPhysicsProxyDriver>>().WithEntityAccess())
        {
            if (m_DriveClientAsDynamic.TryGetComponent(proxyDriver.ValueRO.rootEntity, out DriveClientAsDynamic driveClientAsDynamic))
            {
                commandBuffer.RemoveComponent<DriveClientAsDynamic>(proxyDriver.ValueRO.rootEntity);
                commandBuffer.AddComponent(client, new Simulate {});

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
