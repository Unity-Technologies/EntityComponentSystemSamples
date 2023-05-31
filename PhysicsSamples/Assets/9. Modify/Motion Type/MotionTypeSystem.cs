using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace Modify
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct MotionTypeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            List<RenderMeshArray> renderMeshArraysToAdd = new List<RenderMeshArray>();
            NativeList<Entity> entitiesToAdd = new NativeList<Entity>(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var(renderMeshArray, modifier, materials, entity) in
                     SystemAPI.Query<RenderMeshArray, RefRW<MotionType>, MotionMaterials>()
                         .WithEntityAccess())
            {
                // tick timer
                modifier.ValueRW.Timer -= deltaTime;

                if (modifier.ValueRW.Timer > 0f)
                {
                    continue;
                }

                // reset timer
                modifier.ValueRW.Timer = modifier.ValueRW.TimeLimit;

                var setVelocityToZero = (byte)(modifier.ValueRW.SetVelocityToZero ? 1 : 0);
                // make modifications based on new motion type
                UnityEngine.Material material = renderMeshArray.Materials[0];
                switch (modifier.ValueRW.NewMotionType)
                {
                    case BodyMotionType.Dynamic:
                        // a dynamic body has PhysicsVelocity and PhysicsMassOverride is disabled if it exists
                        if (!SystemAPI.HasComponent<PhysicsVelocity>(entity))
                        {
                            ecb.AddComponent(entity, modifier.ValueRW.DynamicInitialVelocity);
                        }

                        if (SystemAPI.HasComponent<PhysicsMassOverride>(entity))
                        {
                            ecb.SetComponent(entity,
                                new PhysicsMassOverride { IsKinematic = 0, SetVelocityToZero = setVelocityToZero });
                        }

                        material = materials.DynamicMaterial;
                        break;
                    case BodyMotionType.Kinematic:
                        // a static body has PhysicsVelocity and PhysicsMassOverride is enabled if it exists
                        // note that a 'kinematic' body is really just a dynamic body with infinite mass properties
                        // hence you can create a persistently kinematic body by setting properties via PhysicsMass.CreateKinematic()
                        if (!SystemAPI.HasComponent<PhysicsVelocity>(entity))
                        {
                            ecb.AddComponent(entity, modifier.ValueRW.DynamicInitialVelocity);
                        }

                        if (SystemAPI.HasComponent<PhysicsMassOverride>(entity))
                        {
                            ecb.SetComponent(entity,
                                new PhysicsMassOverride { IsKinematic = 1, SetVelocityToZero = setVelocityToZero });
                        }

                        material = materials.KinematicMaterial;
                        break;
                    case BodyMotionType.Static:
                        // a static body is one with a PhysicsCollider but no PhysicsVelocity
                        if (SystemAPI.HasComponent<PhysicsVelocity>(entity))
                        {
                            ecb.RemoveComponent<PhysicsVelocity>(entity);
                        }

                        material = materials.StaticMaterial;
                        break;
                }

                // assign the new render mesh material
                var newRenderMeshArray = new RenderMeshArray(new[] { material }, renderMeshArray.Meshes);
                renderMeshArraysToAdd.Add(newRenderMeshArray);
                entitiesToAdd.Add(entity);

                // move to next motion type
                modifier.ValueRW.NewMotionType = (BodyMotionType)(((int)modifier.ValueRW.NewMotionType + 1) % 3);
            }

            ecb.Playback(state.EntityManager);
            for (int i = 0; i < entitiesToAdd.Length; i++)
            {
                var entity = entitiesToAdd[i];
                var renderMeshArray = renderMeshArraysToAdd[i];

                RenderMeshUtility.AddComponents(entity, state.EntityManager,
                    new RenderMeshDescription(ShadowCastingMode.Off), renderMeshArray,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            }
        }
    }
}
