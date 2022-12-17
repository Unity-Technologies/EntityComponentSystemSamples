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
[BurstCompile]
public partial struct RaycastWithCustomCollectorSystem : ISystem
{
    private ComponentHandles m_Handles;

    struct ComponentHandles
    {
#if !ENABLE_TRANSFORM_V1
        public ComponentLookup<LocalTransform> LocalTransforms;
        public ComponentLookup<PostTransformScale> PostTransformScales;
#else
        public ComponentLookup<Translation> Positions;
        public ComponentLookup<NonUniformScale> NonUniformScales;
#endif

        public ComponentHandles(ref SystemState state)
        {
#if !ENABLE_TRANSFORM_V1
            LocalTransforms = state.GetComponentLookup<LocalTransform>(false);
            PostTransformScales = state.GetComponentLookup<PostTransformScale>(false);
#else
            Positions = state.GetComponentLookup<Translation>(false);
            NonUniformScales = state.GetComponentLookup<NonUniformScale>(false);
#endif
        }

        public void Update(ref SystemState state)
        {
#if !ENABLE_TRANSFORM_V1
            LocalTransforms.Update(ref state);
            PostTransformScales.Update(ref state);
#else
            Positions.Update(ref state);
            NonUniformScales.Update(ref state);
#endif
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VisualizedRaycast>();
        m_Handles = new ComponentHandles(ref state);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public partial struct RaycastWithCustomCollectorJob : IJobEntity
    {
#if !ENABLE_TRANSFORM_V1
        public ComponentLookup<LocalTransform> LocalTransforms;
        public ComponentLookup<PostTransformScale> PostTransformScales;
#else
        public ComponentLookup<Translation> Translations;
        public ComponentLookup<NonUniformScale> NonUniformScales;
#endif
        public PhysicsWorldSingleton PhysicsWorldSingleton;

        [BurstCompile]
#if !ENABLE_TRANSFORM_V1
        public void Execute(Entity entity, ref VisualizedRaycast visualizedRaycast)
        {
            var rayLocalTransform = LocalTransforms[entity];
#else
        public void Execute(Entity entity, ref Rotation rotation, ref VisualizedRaycast visualizedRaycast)
        {
            var position = Translations[entity];
#endif
            var raycastLength = visualizedRaycast.RayLength;

            // Perform the Raycast
            var raycastInput = new RaycastInput
            {
#if !ENABLE_TRANSFORM_V1
                Start = rayLocalTransform.Position,
                End = rayLocalTransform.Position + rayLocalTransform.Forward() * visualizedRaycast.RayLength,
#else
                Start = position.Value,
                End = position.Value + (math.forward(rotation.Value) * visualizedRaycast.RayLength),
#endif
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
            var newHitRayScale = new float3(.05f, .05f, raycastLength * hit.Fraction * 0.5f);
#if !ENABLE_TRANSFORM_V1
            LocalTransforms[visualizedRaycast.HitPositionEntity] = LocalTransforms[visualizedRaycast.HitPositionEntity].WithPosition(newHitPosition);
            LocalTransforms[visualizedRaycast.HitRayEntity] = LocalTransforms[visualizedRaycast.HitRayEntity].WithPosition(newHitRayPosition).WithScale(1);
            PostTransformScales[visualizedRaycast.HitRayEntity] = new PostTransformScale { Value = float3x3.Scale(newHitRayScale) };
            LocalTransforms[visualizedRaycast.FullRayEntity] = LocalTransforms[visualizedRaycast.FullRayEntity].WithPosition(newFullRayPosition).WithScale(1);
            PostTransformScales[visualizedRaycast.FullRayEntity] = new PostTransformScale { Value = float3x3.Scale(newFullRayScale) };
#else
            Translations[visualizedRaycast.HitPositionEntity] = new Translation { Value = newHitPosition };
            Translations[visualizedRaycast.HitRayEntity] = new Translation { Value = newHitRayPosition };
            NonUniformScales[visualizedRaycast.HitRayEntity] = new NonUniformScale { Value = newHitRayScale };
            Translations[visualizedRaycast.FullRayEntity] = new Translation { Value = newFullRayPosition };
            NonUniformScales[visualizedRaycast.FullRayEntity] = new NonUniformScale { Value = newFullRayScale };
#endif
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
#if !ENABLE_TRANSFORM_V1
            LocalTransforms = m_Handles.LocalTransforms,
            PostTransformScales = m_Handles.PostTransformScales,
#else
            Translations = m_Handles.Positions,
            NonUniformScales = m_Handles.NonUniformScales,
#endif
            PhysicsWorldSingleton = physicsWorldSingleton
        };
        state.Dependency = raycastJob.Schedule(state.Dependency);
    }
}
