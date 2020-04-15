using Unity.Burst;
using Unity.Collections;
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

public class RaycastWithCustomCollectorSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var collisionWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld.CollisionWorld;
        var translations = GetComponentDataFromEntity<Translation>();

        Entities.ForEach(
            (Entity entity, 
            ref Translation position, ref Rotation rotation, 
            ref VisualizedRaycast visualizedRaycast) =>
        {
            var raycastLength = visualizedRaycast.RayLength;
            
            // Perform the Raycast
            var raycastInput = new RaycastInput
            {
                Start = position.Value,
                End = position.Value + (math.forward(rotation.Value) * visualizedRaycast.RayLength),
                Filter = CollisionFilter.Default
            };

            using (var raycastHit = new NativeArray<RaycastHit>(1, Allocator.TempJob))
            {
                var raycastJob = new RaycastWithCustomCollectorJob
                {
                    RaycastInput = raycastInput,
                    World = collisionWorld,
                    Hit = raycastHit
                };
                raycastJob.Run();

                var hit = raycastHit[0];
                var hitDistance = raycastLength * hit.Fraction;

                // position the entities and scale based on the ray length and hit distance
                // visualization elements are scaled along the z-axis aka math.forward
                var newFullRayPosition = new float3(0, 0, raycastLength * 0.5f);
                var newFullRayScale = new float3(1f, 1f, raycastLength);
                var newHitPosition = new float3(0, 0, hitDistance);
                var newHitRayPosition = new float3(0, 0, hitDistance * 0.5f);
                var newHitRayScale = new float3(1f, 1f, raycastLength * hit.Fraction);

                PostUpdateCommands.SetComponent(visualizedRaycast.HitPositionEntity, new Translation { Value = newHitPosition });
                PostUpdateCommands.SetComponent(visualizedRaycast.HitRayEntity, new Translation { Value = newHitRayPosition });
                PostUpdateCommands.SetComponent(visualizedRaycast.HitRayEntity, new NonUniformScale { Value = newHitRayScale });
                PostUpdateCommands.SetComponent(visualizedRaycast.FullRayEntity, new Translation { Value = newFullRayPosition });
                PostUpdateCommands.SetComponent(visualizedRaycast.FullRayEntity, new NonUniformScale { Value = newFullRayScale });
            }
        });
    }

    [BurstCompile]
    private struct RaycastWithCustomCollectorJob : IJob
    {
        // Job input
        [ReadOnly] public CollisionWorld World;
        [ReadOnly] public RaycastInput RaycastInput;

        // Job output
        public NativeArray<RaycastHit> Hit;

        public void Execute()
        {
            var collector = new IgnoreTransparentClosestHitCollector(World);

            World.CastRay(RaycastInput, ref collector);
            Hit[0] = collector.ClosestHit;
        }
    }
}
