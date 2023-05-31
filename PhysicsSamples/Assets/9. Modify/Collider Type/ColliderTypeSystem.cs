using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;

namespace Modify
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct ColliderTypeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChangeColliderType>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var materialMeshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>();

            foreach (var(modifier, entity) in
                     SystemAPI.Query<RefRW<ChangeColliderType>>()
                         .WithAll<PhysicsCollider, RenderMeshArray>()
                         .WithEntityAccess())
            {
                modifier.ValueRW.LocalTime -= deltaTime;
                if (modifier.ValueRW.LocalTime > 0.0f)
                {
                    return;
                }
                modifier.ValueRW.LocalTime = modifier.ValueRW.TimeToSwap;

                var collider = state.EntityManager.GetComponentData<PhysicsCollider>(entity);

                unsafe
                {
                    if (collider.ColliderPtr->Type == modifier.ValueRW.ColliderA.ColliderPtr->Type)
                    {
                        ecb.SetComponent(entity, modifier.ValueRW.ColliderB);
                        ecb.SetComponent(entity, materialMeshInfoLookup[modifier.ValueRW.EntityB]);
                    }
                    else
                    {
                        ecb.SetComponent(entity, modifier.ValueRW.ColliderA);
                        ecb.SetComponent(entity, materialMeshInfoLookup[modifier.ValueRW.EntityA]);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
