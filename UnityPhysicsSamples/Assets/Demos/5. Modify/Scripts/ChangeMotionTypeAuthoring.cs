using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;

public struct ChangeMotionType : IComponentData
{
    public BodyMotionType NewMotionType;
    public PhysicsVelocity DynamicInitialVelocity;
    public float TimeLimit;
    internal float Timer;
}

public struct ChangeMotionMaterials : ISharedComponentData, IEquatable<ChangeMotionMaterials>
{
    public UnityEngine.Material DynamicMaterial;
    public UnityEngine.Material KinematicMaterial;
    public UnityEngine.Material StaticMaterial;

    public bool Equals(ChangeMotionMaterials other) =>
        Equals(DynamicMaterial, other.DynamicMaterial)
        && Equals(KinematicMaterial, other.KinematicMaterial)
        && Equals(StaticMaterial, other.StaticMaterial);

    public override bool Equals(object obj) => obj is ChangeMotionMaterials other && Equals(other);

    public override int GetHashCode() =>
        unchecked((int)math.hash(new int3(
            DynamicMaterial != null ? DynamicMaterial.GetHashCode() : 0,
            KinematicMaterial != null ? KinematicMaterial.GetHashCode() : 0,
            StaticMaterial != null ? StaticMaterial.GetHashCode() : 0
        )));
}

// Converted in PhysicsSamplesConversionSystem so Physics and Graphics conversion is complete
public class ChangeMotionTypeAuthoring : MonoBehaviour//, IConvertGameObjectToEntity
{
    public UnityEngine.Material DynamicMaterial;
    public UnityEngine.Material KinematicMaterial;
    public UnityEngine.Material StaticMaterial;

    [Range(0, 10)] public float TimeToSwap = 1.0f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ChangeMotionType
        {
            NewMotionType = BodyMotionType.Dynamic,
            DynamicInitialVelocity = dstManager.GetComponentData<PhysicsVelocity>(entity),
            TimeLimit = TimeToSwap,
            Timer = TimeToSwap
        });

        dstManager.AddSharedComponentData(entity, new ChangeMotionMaterials
        {
            DynamicMaterial = DynamicMaterial,
            KinematicMaterial = KinematicMaterial,
            StaticMaterial = StaticMaterial
        });
        dstManager.AddComponent<PhysicsMassOverride>(entity);
    }
}

[UpdateBefore(typeof(BuildPhysicsWorld))]
public class ChangeMotionTypeSystem : SystemBase
{
    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate() =>
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();

        var deltaTime = UnityEngine.Time.fixedDeltaTime;

        Entities
            .WithName("ChangeMotionTypeJob")
            .WithoutBurst()
            .ForEach((Entity entity, ref ChangeMotionType modifier, in ChangeMotionMaterials materials, in RenderMesh renderMesh) =>
            {
                // tick timer
                modifier.Timer -= deltaTime;

                if (modifier.Timer > 0f)
                    return;

                // reset timer
                modifier.Timer = modifier.TimeLimit;

                // make modifications based on new motion type
                UnityEngine.Material material = renderMesh.material;
                switch (modifier.NewMotionType)
                {
                    case BodyMotionType.Dynamic:
                        // a dynamic body has PhysicsVelocity and PhysicsMassOverride is disabled if it exists
                        if (!HasComponent<PhysicsVelocity>(entity))
                            commandBuffer.AddComponent(entity, modifier.DynamicInitialVelocity);
                        if (HasComponent<PhysicsMassOverride>(entity))
                            commandBuffer.SetComponent(entity, new PhysicsMassOverride { IsKinematic = 0 });

                        material = materials.DynamicMaterial;
                        break;
                    case BodyMotionType.Kinematic:
                        // a static body has PhysicsVelocity and PhysicsMassOverride is enabled if it exists
                        // note that a 'kinematic' body is really just a dynamic body with infinite mass properties
                        // hence you can create a persistently kinematic body by setting properties via PhysicsMass.CreateKinematic()
                        if (!HasComponent<PhysicsVelocity>(entity))
                            commandBuffer.AddComponent(entity, modifier.DynamicInitialVelocity);
                        if (HasComponent<PhysicsMassOverride>(entity))
                            commandBuffer.SetComponent(entity, new PhysicsMassOverride { IsKinematic = 1 });

                        material = materials.KinematicMaterial;
                        break;
                    case BodyMotionType.Static:
                        // a static body is one with a PhysicsCollider but no PhysicsVelocity
                        if (HasComponent<PhysicsVelocity>(entity))
                            commandBuffer.RemoveComponent<PhysicsVelocity>(entity);

                        material = materials.StaticMaterial;
                        break;
                }

                // assign the new render mesh material
                var newRenderMesh = renderMesh;
                newRenderMesh.material = material;
                commandBuffer.SetSharedComponent(entity, newRenderMesh);

                // move to next motion type
                modifier.NewMotionType = (BodyMotionType)(((int)modifier.NewMotionType + 1) % 3);
            }).Run();

        m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
