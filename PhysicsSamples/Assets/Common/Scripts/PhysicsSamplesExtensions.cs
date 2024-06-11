using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


namespace Unity.Physics.Extensions
{
    /// <summary>
    /// Utility functions acting on physics components.
    /// </summary>
    public static class PhysicsSamplesExtensions
    {
        #region CompoundCollider Utilities
        /// <summary>
        /// Given the root Collider of a hierarchy and a ColliderKey referencing a child in that hierarchy,
        /// this function returns the ColliderKey referencing the parent Collider.
        /// </summary>
        /// <param name="rootColliderPtr">A <see cref="Collider"/> at the root of a Collider hierarchy.</param>
        /// <param name="childColliderKey">A <see cref="ColliderKey"/> referencing a child Collider somewhere in the hierarchy below rootColliderPtr.</param>
        /// <param name="parentColliderKey">A <see cref="ColliderKey"/> referencing the parent of the child Collider. Will be ColliderKey.Empty if the parameters where invalid.</param>
        /// <returns>Whether the parent was successfully found in the hierarchy.</returns>
        public static unsafe bool TryGetParentColliderKey(Collider* rootColliderPtr, ColliderKey childColliderKey, out ColliderKey parentColliderKey)
        {
            var childColliderPtr = rootColliderPtr;
            var childColliderKeyNumBits = childColliderPtr->NumColliderKeyBits;

            // Start with an Empty collider key and push sub keys onto it as we traverse down the compound hierarchy.
            var parentColliderKeyPath = ColliderKeyPath.Empty;

            // On the way down, the childColliderKey pops of sub keys and pushes them onto the parentColliderKeyPath
            do
            {
                childColliderKey.PopSubKey(childColliderKeyNumBits, out var childIndex);
                switch (childColliderPtr->Type)
                {
                    case ColliderType.Compound:
                        // Get the next child down and loop again
                        parentColliderKeyPath.PushChildKey(new ColliderKeyPath(new ColliderKey(childColliderKeyNumBits, childIndex), childColliderKeyNumBits));
                        childColliderPtr = ((CompoundCollider*)childColliderPtr)->Children[(int)childIndex].Collider;
                        childColliderKeyNumBits = childColliderPtr->NumColliderKeyBits;
                        break;
                    case ColliderType.Mesh:
                    case ColliderType.Terrain:
                        // We've hit a Terrain or Mesh collider so there should only be PolygonColliders below this.
                        // At this point the childColliderKey should be Empty and childIndex should be the index of the polygon.
                        if (!childColliderKey.Equals(ColliderKey.Empty))
                        {
                            // We've reached the bottom without popping all the child keys.
                            // The given childColliderKey doesn't fit this hierarchy!
                            parentColliderKey = ColliderKey.Empty;
                            return false;
                        }
                        break;
                    default:
                        // We've hit a Convex collider, so rootColliderPtr must not have been
                        // the root of a hierarchy in the first place and so there is no parent!
                        parentColliderKey = ColliderKey.Empty;
                        return false;
                }
            }
            while (!childColliderKey.Equals(ColliderKey.Empty)
                   && !childColliderPtr->CollisionType.Equals(CollisionType.Convex));

            parentColliderKey = parentColliderKeyPath.Key;
            // childColliderKey should be Empty at this point.
            // However, if it isn't then we reached a leaf without finding the child collider!
            return childColliderKey.Equals(ColliderKey.Empty);
        }

        /// <summary>
        /// Given the root Collider of a hierarchy and a ColliderKey referencing a child in that hierarchy,
        /// this function returns the ChildCollider requested.
        /// </summary>
        /// <param name="rootColliderPtr">A <see cref="Collider"/> at the root of a Collider hierarchy.</param>
        /// <param name="childColliderKey">A <see cref="ColliderKey"/> referencing a child Collider somewhere in the hierarchy below rootColliderPtr.</param>
        /// <param name="childCollider">A valid <see cref="ChildCollider"/> returned from the hierarchy, if found.</param>
        /// <returns>Whether a specified ColliderKey was successfully found in the hierarchy.</returns>
        public static unsafe bool TryGetChildInHierarchy(Collider* rootColliderPtr, ColliderKey childColliderKey, out ChildCollider childCollider)
        {
            //public static unsafe bool GetLeafCollider(Collider* root, RigidTransform rootTransform, ColliderKey key, out ChildCollider leaf)
            childCollider = new ChildCollider(rootColliderPtr, RigidTransform.identity);
            while (!childColliderKey.Equals(ColliderKey.Empty))
            {
                if (!childCollider.Collider->GetChild(ref childColliderKey, out ChildCollider child))
                {
                    break;
                }
                childCollider = new ChildCollider(childCollider, child);
            }
            return (childCollider.Collider != null);
        }

        /// <summary>
        /// Sets the Entity references in a CompoundCollider according to the provided collider key entity pairs.
        /// </summary>
        /// <param name="compoundColliderPtr">A <see cref="CompoundCollider"/>.</param>
        /// <param name="keyEntityPairs">An array of <see cref="ColliderKey"/> and <see cref="Entity"/> pairs.</param>
        public static unsafe void RemapColliderEntityReferences(
            CompoundCollider* compoundColliderPtr, in NativeArray<PhysicsColliderKeyEntityPair> keyEntityPairs) =>
            RemapCompoundColliderEntityReferences(compoundColliderPtr, keyEntityPairs, ColliderKey.Empty);

        internal static unsafe void RemapCompoundColliderEntityReferences(
            CompoundCollider* compoundColliderPtr,
            in NativeArray<PhysicsColliderKeyEntityPair> keyEntityPairs,
            in ColliderKey key)
        {
            for (int childIndex = 0; childIndex < compoundColliderPtr->Children.Length; childIndex++)
            {
                ref CompoundCollider.Child child = ref compoundColliderPtr->Children[childIndex];
                var childKey = key; childKey.PushSubKey(compoundColliderPtr->NumColliderKeyBits, (uint)childIndex);
                for (int i = 0; i < keyEntityPairs.Length; i++)
                {
                    if (childKey.Equals(keyEntityPairs[i].Key))
                    {
                        child.Entity = keyEntityPairs[i].Entity;
                        break;
                    }
                }
                if (child.Collider->Type == ColliderType.Compound)
                {
                    RemapCompoundColliderEntityReferences((CompoundCollider*)child.Collider, keyEntityPairs, childKey);
                }
            }
        }

        #endregion
    }
}
