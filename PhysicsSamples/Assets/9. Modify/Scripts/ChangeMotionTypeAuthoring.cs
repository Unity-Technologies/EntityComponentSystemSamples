using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

public struct ChangeMotionType : IComponentData
{
    public BodyMotionType NewMotionType;
    public PhysicsVelocity DynamicInitialVelocity;
    public float TimeLimit;
    public bool SetVelocityToZero;
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

public class ChangeMotionTypeAuthoring : MonoBehaviour
{
    public UnityEngine.Material DynamicMaterial;
    public UnityEngine.Material KinematicMaterial;
    public UnityEngine.Material StaticMaterial;

    [Range(0, 10)] public float TimeToSwap = 1.0f;
    public bool SetVelocityToZero = false;
}

class ChangeMotionTypeAuthoringBaker : Baker<ChangeMotionTypeAuthoring>
{
    public override void Bake(ChangeMotionTypeAuthoring authoring)
    {
        var velocity = new PhysicsVelocity();
        var physicsBodyAuthoring = GetComponent<PhysicsBodyAuthoring>();
        if (physicsBodyAuthoring != null)
        {
            velocity.Linear = physicsBodyAuthoring.InitialLinearVelocity;
            velocity.Angular = physicsBodyAuthoring.InitialAngularVelocity;
        }

        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ChangeMotionType
        {
            NewMotionType = BodyMotionType.Dynamic,
            DynamicInitialVelocity = velocity,
            TimeLimit = authoring.TimeToSwap,
            Timer = authoring.TimeToSwap,
            SetVelocityToZero = authoring.SetVelocityToZero
        });

        AddSharedComponentManaged(entity, new ChangeMotionMaterials
        {
            DynamicMaterial = authoring.DynamicMaterial,
            KinematicMaterial = authoring.KinematicMaterial,
            StaticMaterial = authoring.StaticMaterial
        });
        AddComponent<PhysicsMassOverride>(entity);
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct ChangeMotionTypeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        List<RenderMeshArray> renderMeshArraysToAdd = new List<RenderMeshArray>();
        NativeList<Entity> entitiesToAdd = new NativeList<Entity>(Allocator.Temp);
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var(renderMeshArray, modifier, materials, entity) in SystemAPI.Query<RenderMeshArray, RefRW<ChangeMotionType>, ChangeMotionMaterials>().WithEntityAccess())
        {
            // tick timer
            modifier.ValueRW.Timer -= deltaTime;

            if (modifier.ValueRW.Timer > 0f)
                continue;

            // reset timer
            modifier.ValueRW.Timer = modifier.ValueRW.TimeLimit;

            var setVelocityToZero = (byte)(modifier.ValueRW.SetVelocityToZero ? 1 : 0);
            // make modifications based on new motion type
            UnityEngine.Material material = renderMeshArray.MaterialReferences[0];
            switch (modifier.ValueRW.NewMotionType)
            {
                case BodyMotionType.Dynamic:
                    // a dynamic body has PhysicsVelocity and PhysicsMassOverride is disabled if it exists
                    if (!SystemAPI.HasComponent<PhysicsVelocity>(entity))
                        commandBuffer.AddComponent(entity, modifier.ValueRW.DynamicInitialVelocity);
                    if (SystemAPI.HasComponent<PhysicsMassOverride>(entity))
                        commandBuffer.SetComponent(entity, new PhysicsMassOverride { IsKinematic = 0, SetVelocityToZero = setVelocityToZero });

                    material = materials.DynamicMaterial;
                    break;
                case BodyMotionType.Kinematic:
                    // a static body has PhysicsVelocity and PhysicsMassOverride is enabled if it exists
                    // note that a 'kinematic' body is really just a dynamic body with infinite mass properties
                    // hence you can create a persistently kinematic body by setting properties via PhysicsMass.CreateKinematic()
                    if (!SystemAPI.HasComponent<PhysicsVelocity>(entity))
                        commandBuffer.AddComponent(entity, modifier.ValueRW.DynamicInitialVelocity);
                    if (SystemAPI.HasComponent<PhysicsMassOverride>(entity))
                        commandBuffer.SetComponent(entity, new PhysicsMassOverride { IsKinematic = 1, SetVelocityToZero = setVelocityToZero });

                    material = materials.KinematicMaterial;
                    break;
                case BodyMotionType.Static:
                    // a static body is one with a PhysicsCollider but no PhysicsVelocity
                    if (SystemAPI.HasComponent<PhysicsVelocity>(entity))
                        commandBuffer.RemoveComponent<PhysicsVelocity>(entity);

                    material = materials.StaticMaterial;
                    break;
            }

            // assign the new render mesh material
            var materialArray = new[] { (UnityObjectRef<Material>)material };
            var newRenderMeshArray = new RenderMeshArray(materialArray, renderMeshArray.MeshReferences);
            renderMeshArraysToAdd.Add(newRenderMeshArray);
            entitiesToAdd.Add(entity);

            // move to next motion type
            modifier.ValueRW.NewMotionType = (BodyMotionType)(((int)modifier.ValueRW.NewMotionType + 1) % 3);
        }

        commandBuffer.Playback(state.EntityManager);
        for (int i = 0; i < entitiesToAdd.Length; i++)
        {
            var entity = entitiesToAdd[i];
            var renderMeshArray = renderMeshArraysToAdd[i];

            RenderMeshUtility.AddComponents(entity, state.EntityManager, new RenderMeshDescription(ShadowCastingMode.Off), renderMeshArray, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        }
    }
}
