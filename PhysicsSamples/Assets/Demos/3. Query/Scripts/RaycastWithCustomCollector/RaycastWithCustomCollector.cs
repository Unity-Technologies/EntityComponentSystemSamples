using Unity.Assertions;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using Collider = Unity.Physics.Collider;
using RaycastHit = Unity.Physics.RaycastHit;

// This collector filters out bodies with transparent custom tag
public struct IgnoreTransparentClosestHitCollector : ICollector<RaycastHit>
{
    public bool EarlyOutOnFirstHit => false;

    public float MaxFraction {get; private set;}

    public int NumHits { get; private set; }

    public RaycastHit ClosestHit;

    private CollisionWorld m_World;
    private const int k_TransparentCustomTag = (1 << 1);

    public IgnoreTransparentClosestHitCollector(CollisionWorld world)
    {
        m_World = world;

        MaxFraction = 1.0f;
        ClosestHit = default;
        NumHits = 0;
    }

    private static bool IsTransparent(BlobAssetReference<Collider> collider, ColliderKey key)
    {
        bool bIsTransparent = false;
        unsafe
        {
            // Only Convex Colliders have Materials associated with them. So base on CollisionType
            // we'll need to cast from the base Collider type, hence, we need the pointer.
            var c = collider.AsPtr();
            {
                var cc = ((ConvexCollider*)c);

                // We also need to check if our Collider is Composite (i.e. has children).
                // If it is then we grab the actual leaf node hit by the ray.
                // Checking if our collider is composite
                if (c->CollisionType != CollisionType.Convex)
                {
                    // If it is, get the leaf as a Convex Collider
                    c->GetLeaf(key, out ChildCollider child);
                    cc = (ConvexCollider*)child.Collider;
                }

                // Now we've definitely got a ConvexCollider so can check the Material.
                bIsTransparent = (cc->Material.CustomTags & k_TransparentCustomTag) != 0;
            }
        }

        return bIsTransparent;
    }

    public bool AddHit(RaycastHit hit)
    {
        if (IsTransparent(m_World.Bodies[hit.RigidBodyIndex].Collider, hit.ColliderKey))
        {
            return false;
        }

        MaxFraction = hit.Fraction;
        ClosestHit = hit;
        NumHits = 1;

        return true;
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct RaycastWithCustomCollectorSystem : ISystem
{
    private ComponentHandles m_Handles;

    struct ComponentHandles
    {
        public ComponentLookup<LocalTransform> LocalTransforms;
        public ComponentLookup<PostTransformMatrix> PostTransformMatrices;

        public ComponentHandles(ref SystemState state)
        {
            LocalTransforms = state.GetComponentLookup<LocalTransform>(false);
            PostTransformMatrices = state.GetComponentLookup<PostTransformMatrix>(false);
        }

        public void Update(ref SystemState state)
        {
            LocalTransforms.Update(ref state);
            PostTransformMatrices.Update(ref state);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VisualizedRaycast>();
        m_Handles = new ComponentHandles(ref state);
    }

    [BurstCompile]
    public partial struct RaycastWithCustomCollectorJob : IJobEntity
    {
        public ComponentLookup<LocalTransform> LocalTransforms;
        public ComponentLookup<PostTransformMatrix> PostTransformMatrices;

        [Unity.Collections.ReadOnly]
        public PhysicsWorldSingleton PhysicsWorldSingleton;

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


            LocalTransforms[visualizedRaycast.HitPositionEntity] = LocalTransforms[visualizedRaycast.HitPositionEntity].WithPosition(newHitPosition);
            LocalTransforms[visualizedRaycast.HitRayEntity] = LocalTransforms[visualizedRaycast.HitRayEntity].WithPosition(newHitRayPosition).WithScale(1);
            PostTransformMatrices[visualizedRaycast.HitRayEntity] = new PostTransformMatrix { Value = float4x4.Scale(newHitRayScale) };
            LocalTransforms[visualizedRaycast.FullRayEntity] = LocalTransforms[visualizedRaycast.FullRayEntity].WithPosition(newFullRayPosition).WithScale(1);
            PostTransformMatrices[visualizedRaycast.FullRayEntity] = new PostTransformMatrix { Value = float4x4.Scale(newFullRayScale) };
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_Handles.Update(ref state);

        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var world = physicsWorldSingleton.CollisionWorld;

        var raycastJob = new RaycastWithCustomCollectorJob
        {
            LocalTransforms = m_Handles.LocalTransforms,
            PostTransformMatrices = m_Handles.PostTransformMatrices,
            PhysicsWorldSingleton = physicsWorldSingleton
        };
        state.Dependency = raycastJob.Schedule(state.Dependency);
    }
}
