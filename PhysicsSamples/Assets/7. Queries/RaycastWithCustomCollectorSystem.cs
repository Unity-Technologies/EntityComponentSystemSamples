using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Query
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct RaycastWithCustomCollectorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VisualizedRaycast>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var world = physicsWorldSingleton.CollisionWorld;

            var raycastJob = new RaycastWithCustomCollectorJob
            {
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(false),
                PostTransformMatrices = SystemAPI.GetComponentLookup<PostTransformMatrix>(false),
                PhysicsWorldSingleton = physicsWorldSingleton
            };
            state.Dependency = raycastJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct RaycastWithCustomCollectorJob : IJobEntity
        {
            public ComponentLookup<LocalTransform> LocalTransforms;
            public ComponentLookup<PostTransformMatrix> PostTransformMatrices;

            [Unity.Collections.ReadOnly] public PhysicsWorldSingleton PhysicsWorldSingleton;

            public void Execute(Entity entity, ref VisualizedRaycast visualizedRaycast)
            {
                var rayLocalTransform = LocalTransforms[entity];
                var raycastLength = visualizedRaycast.RayLength;

                // Perform the Raycast
                var raycastInput = new RaycastInput
                {
                    Start = rayLocalTransform.Position,
                    End = rayLocalTransform.Position + rayLocalTransform.Forward() * visualizedRaycast.RayLength,

                    Filter = CollisionFilter.Default
                };

                var collector = new IgnoreTransparentClosestHitCollector(PhysicsWorldSingleton.CollisionWorld);
                PhysicsWorldSingleton.CastRay(raycastInput, ref collector);
                var hit = collector.ClosestHit;
                var hitDistance = raycastLength * hit.Fraction;

                // position the entities and scale based on the ray length and hit distance
                // visualization elements are scaled along the z-axis aka math.forward
                var newFullRayPosition = new float3(0, 0, raycastLength * 0.5f);
                var newHitPosition = new float3(0, 0, hitDistance);
                var newHitRayPosition = new float3(0, 0, hitDistance * 0.5f);
                var newFullRayScale = new float3(.025f, .025f, raycastLength * 0.5f);
                var newHitRayScale = new float3(0.1f, 0.1f, raycastLength * hit.Fraction);

                LocalTransforms[visualizedRaycast.HitPositionEntity] =
                    LocalTransforms[visualizedRaycast.HitPositionEntity].WithPosition(newHitPosition);
                LocalTransforms[visualizedRaycast.HitRayEntity] = LocalTransforms[visualizedRaycast.HitRayEntity]
                    .WithPosition(newHitRayPosition).WithScale(1);
                PostTransformMatrices[visualizedRaycast.HitRayEntity] = new PostTransformMatrix
                {
                    Value = float4x4.Scale(newHitRayScale)
                };
                LocalTransforms[visualizedRaycast.FullRayEntity] = LocalTransforms[visualizedRaycast.FullRayEntity]
                    .WithPosition(newFullRayPosition).WithScale(1);
                PostTransformMatrices[visualizedRaycast.FullRayEntity] = new PostTransformMatrix
                {
                    Value = float4x4.Scale(newFullRayScale)
                };
            }
        }
    }
}
