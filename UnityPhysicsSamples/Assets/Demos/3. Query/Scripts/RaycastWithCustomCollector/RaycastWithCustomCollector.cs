using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
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
            var c = (Collider*)collider.GetUnsafePtr();
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

[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class RaycastWithCustomCollectorSystem : SystemBase
{
    BuildPhysicsWorld m_BuildPhysicsWorld;
    EndFramePhysicsSystem m_EndFramePhysicsSystem;
    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        CollisionWorld collisionWorld = m_BuildPhysicsWorld.PhysicsWorld.CollisionWorld;
        EntityCommandBuffer commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
        Dependency = JobHandle.CombineDependencies(Dependency, m_EndFramePhysicsSystem.FinalJobHandle);

        Entities
            .WithName("RaycastWithCustomCollector")
            .WithBurst()
            .ForEach((Entity entity, ref Translation position, ref Rotation rotation, ref VisualizedRaycast visualizedRaycast) =>
        {
            var raycastLength = visualizedRaycast.RayLength;
            
            // Perform the Raycast
            var raycastInput = new RaycastInput
            {
                Start = position.Value,
                End = position.Value + (math.forward(rotation.Value) * visualizedRaycast.RayLength),
                Filter = CollisionFilter.Default
            };

            var collector = new IgnoreTransparentClosestHitCollector(collisionWorld);

            collisionWorld.CastRay(raycastInput, ref collector);

            var hit = collector.ClosestHit;
            var hitDistance = raycastLength * hit.Fraction;

            // position the entities and scale based on the ray length and hit distance
            // visualization elements are scaled along the z-axis aka math.forward
            var newFullRayPosition = new float3(0, 0, raycastLength * 0.5f);
            var newFullRayScale = new float3(1f, 1f, raycastLength);
            var newHitPosition = new float3(0, 0, hitDistance);
            var newHitRayPosition = new float3(0, 0, hitDistance * 0.5f);
            var newHitRayScale = new float3(1f, 1f, raycastLength * hit.Fraction);

            commandBuffer.SetComponent(visualizedRaycast.HitPositionEntity, new Translation { Value = newHitPosition });
            commandBuffer.SetComponent(visualizedRaycast.HitRayEntity, new Translation { Value = newHitRayPosition });
            commandBuffer.SetComponent(visualizedRaycast.HitRayEntity, new NonUniformScale { Value = newHitRayScale });
            commandBuffer.SetComponent(visualizedRaycast.FullRayEntity, new Translation { Value = newFullRayPosition });
            commandBuffer.SetComponent(visualizedRaycast.FullRayEntity, new NonUniformScale { Value = newFullRayScale });
        }).Schedule();

        m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
