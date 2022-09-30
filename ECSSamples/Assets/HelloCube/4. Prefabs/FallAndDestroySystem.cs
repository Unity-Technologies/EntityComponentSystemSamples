using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.Prefabs
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PrefabsGroup))]
    [BurstCompile]
    public partial struct FallAndDestroySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // An EntityCommandBuffer created from an EntityCommandBufferSystem singleton will be
            // played back and disposed by the EntityCommandBufferSystem the next time it updates.
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Downward vector
            var movement = new float3(0, -SystemAPI.Time.DeltaTime * 5f, 0);

            // WithAll() includes RotationSpeed in the query, but
            // the RotationSpeed component values will not be accessed.
            // WithEntityAccess() includes the Entity ID as the last element of the tuple.
            foreach (var (transform, entity) in
                     SystemAPI.Query<TransformAspect>().WithAll<RotationSpeed>().WithEntityAccess())
            {
                transform.TranslateLocal(movement);
                if (transform.LocalPosition.y < 0)
                {
                    // Making a structural change would invalidate the query we are iterating through,
                    // so instead we record a command to destroy the entity later.
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
