using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Collider = Unity.Physics.Collider;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Query
{
// This collector filters out bodies with transparent custom tag
    public struct IgnoreTransparentClosestHitCollector : ICollector<RaycastHit>
    {
        public bool EarlyOutOnFirstHit => false;

        public float MaxFraction { get; private set; }

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
}
