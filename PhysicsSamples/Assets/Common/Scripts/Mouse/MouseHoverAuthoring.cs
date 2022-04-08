using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static Unity.Physics.Extensions.PhysicsSamplesExtensions;

namespace Unity.Physics.Extensions
{
    public struct MouseHover : ISharedComponentData, IEquatable<MouseHover>
    {
        public bool IgnoreTriggers;
        public bool IgnoreStatic;
        public Entity PreviousEntity;
        public Entity CurrentEntity;
        public UnityEngine.Material HoverMaterial;
        public UnityEngine.Material OriginalMaterial;

        public bool Equals(MouseHover other) =>
            Equals(PreviousEntity, other.PreviousEntity)
            && Equals(CurrentEntity, other.CurrentEntity)
            && Equals(HoverMaterial, other.HoverMaterial)
            && Equals(OriginalMaterial, other.OriginalMaterial);

        public override bool Equals(object obj) => obj is MouseHover other && Equals(other);

        public override int GetHashCode() =>
            unchecked((int)math.hash(new int4x2(
                new int4(
                    IgnoreTriggers ? 1 : 0,
                    IgnoreStatic ? 1 : 0,
                    PreviousEntity.GetHashCode(),
                    CurrentEntity.GetHashCode()),
                new int4(
                    HoverMaterial != null ? HoverMaterial.GetHashCode() : 0,
                    OriginalMaterial != null ? OriginalMaterial.GetHashCode() : 0,
                    0, 0))
            ));
    }

    [DisallowMultipleComponent]
    public class MouseHoverAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public UnityEngine.Material Highlight;
        public bool IgnoreTriggers = true;
        public bool IgnoreStatic = true;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddSharedComponentData(entity, new MouseHover()
            {
                PreviousEntity = Entity.Null,
                CurrentEntity = Entity.Null,
                HoverMaterial = Highlight,
                IgnoreTriggers = IgnoreTriggers,
                IgnoreStatic = IgnoreStatic
            });
        }

        protected void OnEnable() {}
    }

    // Applies any mouse spring as a change in velocity on the entity's motion component
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class MouseHoverSystem : SystemBase
    {
        BuildPhysicsWorld m_BuildPhysicsWorld;

        [BurstCompile]
        public struct WorldRaycastJob : IJob
        {
            public RaycastInput RayInput;
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public bool IgnoreTriggers;
            [ReadOnly] public bool IgnoreStatic;

            public NativeReference<RaycastHit> RaycastHitRef;

            public void Execute()
            {
                var mousePickCollector = new MousePickCollector(1.0f, CollisionWorld.Bodies, CollisionWorld.NumDynamicBodies);
                mousePickCollector.IgnoreTriggers = IgnoreTriggers;
                mousePickCollector.IgnoreStatic = IgnoreStatic;

                if (CollisionWorld.CastRay(RayInput, ref mousePickCollector))
                {
                    RaycastHitRef.Value = mousePickCollector.Hit;
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(MouseHover) }
            }));
        }

        // Find the Entity holding the Graphical representation of a Physics Shape.
        // It may be that the Physics and Graphics representation are on the same Entity.
        public Entity FindGraphicsEntityFromPhysics(Entity bodyEntity) => FindGraphicsEntityFromPhysics(bodyEntity, ColliderKey.Empty);
        public Entity FindGraphicsEntityFromPhysics(Entity bodyEntity, ColliderKey leafColliderKey)
        {
            if (bodyEntity.Equals(Entity.Null))
            {
                // No Physics so no Graphics
                return Entity.Null;
            }

            // Set the Graphics Entity to the supplied Physics Entity
            var renderEntity = bodyEntity;

            // Check if we have hit a leaf node
            if (!leafColliderKey.Equals(ColliderKey.Empty))
            {
                // Get the Physics Collider
                var rootCollider = EntityManager.GetComponentData<PhysicsCollider>(bodyEntity).Value;

                // If we hit a CompoundCollider we need to find the original Entity associated
                // the actual leaf Collider that was hit.
                if (rootCollider.Value.Type == ColliderType.Compound)
                {
                    #region Find a Leaf Entity and ColliderKey
                    var leafEntity = Entity.Null;
                    unsafe
                    {
                        var rootColliderPtr = rootCollider.AsPtr();

                        // Get the leaf Collider and check if we hit was a PolygonCollider (i.e. a Triangle or a Quad)
                        rootColliderPtr->GetLeaf(leafColliderKey, out var childCollider);
                        leafEntity = childCollider.Entity;

                        // PolygonColliders are likely to not have an original Entity associated with them
                        // So if we have a Polygon and it has no Entity then we really need to check for
                        // the higher level Mesh or Terrain Collider instead.
                        var childColliderType = childCollider.Collider->Type;
                        var childColliderIsPolygon = childColliderType == ColliderType.Triangle || childColliderType == ColliderType.Quad;
                        if (childColliderIsPolygon && childCollider.Entity.Equals(Entity.Null))
                        {
                            // Get the ColliderKey of the Polygon's parent
                            if (TryGetParentColliderKey(rootColliderPtr, leafColliderKey, out leafColliderKey))
                            {
                                // Get the Mesh or Terrain Collider of the Polygon
                                TryGetChildInHierarchy(rootColliderPtr, leafColliderKey, out childCollider);
                                leafEntity = childCollider.Entity;
                            }
                        }
                    }
                    #endregion

                    // The Entities recorded in the leaves of a CompoundCollider may have been correct
                    // at the time of conversion. However, if the Collider blob is shared, or came up
                    // through a sub scene, we cannot assume that the baked Entities in the
                    // CompoundCollider are still valid.

                    // On conversion Entities using a CompoundCollider have an extra dynamic buffer added
                    // which holds a list of Entity/ColliderKey pairs. This buffer should be patched up
                    // automatically and be valid with each instance, at least until you start messing
                    // with the Entity hierarchy yourself e.g. by deleting Entities.

                    #region Check the Leaf Entity is valid
                    // If the leafEntity was never assigned in the first place
                    // there is no point in looking up any Buffers.
                    if (!leafEntity.Equals(Entity.Null))
                    {
                        // Check for an Key/Entity pair buffer first.
                        // This should exist if the Physics conversion pipeline was invoked.
                        var colliderKeyEntityPairBuffers = GetBufferFromEntity<PhysicsColliderKeyEntityPair>(true);
                        if (colliderKeyEntityPairBuffers.HasComponent(bodyEntity))
                        {
                            var colliderKeyEntityBuffer = colliderKeyEntityPairBuffers[bodyEntity];
                            // TODO: Faster lookup option?
                            for (int i = 0; i < colliderKeyEntityBuffer.Length; i++)
                            {
                                var bufferColliderKey = colliderKeyEntityBuffer[i].Key;
                                if (leafColliderKey.Equals(bufferColliderKey))
                                {
                                    renderEntity = colliderKeyEntityBuffer[i].Entity;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // We haven't found a Key/Entity pair buffer so the compound collider
                            // may have been created in code.

                            // We'll assume the Entity in the CompoundCollider is valid
                            renderEntity = leafEntity;

                            // If this CompoundCollider was instanced from a prefab then the entities
                            // in the compound children would actually reference the original prefab hierarchy.
                            var rootEntityFromLeaf = leafEntity;
                            while (HasComponent<Parent>(rootEntityFromLeaf))
                            {
                                rootEntityFromLeaf = GetComponent<Parent>(rootEntityFromLeaf).Value;
                            }

                            // If the root Entity found from the leaf does not match the body Entity
                            // then we have hit an instance using the same CompoundCollider.
                            // This means we can try and remap the leaf Entity to the new hierarchy.
                            if (!rootEntityFromLeaf.Equals(bodyEntity))
                            {
                                // This assumes there is a LinkedEntityGroup Buffer on original and instance Entity.
                                // No doubt there is a more optimal way of doing this remap with more specific
                                // knowledge of the final application.
                                var linkedEntityGroupBuffers = GetBufferFromEntity<LinkedEntityGroup>(true);

                                // Only remap if the buffers exist, have been created and are of equal length.
                                bool hasBufferRootEntity = linkedEntityGroupBuffers.HasComponent(rootEntityFromLeaf);
                                bool hasBufferBodyEntity = linkedEntityGroupBuffers.HasComponent(bodyEntity);
                                if (hasBufferRootEntity && hasBufferBodyEntity)
                                {
                                    var prefabEntityGroupBuffer = linkedEntityGroupBuffers[rootEntityFromLeaf];
                                    var instanceEntityGroupBuffer = linkedEntityGroupBuffers[bodyEntity];

                                    if (prefabEntityGroupBuffer.IsCreated && instanceEntityGroupBuffer.IsCreated
                                        && (prefabEntityGroupBuffer.Length == instanceEntityGroupBuffer.Length))
                                    {
                                        var prefabEntityGroup = prefabEntityGroupBuffer.AsNativeArray();
                                        var instanceEntityGroup = instanceEntityGroupBuffer.AsNativeArray();

                                        for (int i = 0; i < prefabEntityGroup.Length; i++)
                                        {
                                            // If we've found the renderEntity index in the prefab hierarchy,
                                            // set the renderEntity to the equivalent Entity in the instance
                                            if (prefabEntityGroup[i].Value.Equals(renderEntity))
                                            {
                                                renderEntity = instanceEntityGroup[i].Value;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
            }

            // Finally check to see if we have a graphics redirection on the shape Entity.
            if (HasComponent<PhysicsRenderEntity>(renderEntity))
            {
                renderEntity = GetComponent<PhysicsRenderEntity>(renderEntity).Entity;
            }

            return renderEntity;
        }

        protected override void OnUpdate()
        {
            var collisionWorld = m_BuildPhysicsWorld.PhysicsWorld.CollisionWorld;
            Vector2 mousePosition = Input.mousePosition;
            UnityEngine.Ray unityRay = Camera.main.ScreenPointToRay(mousePosition);
            var rayInput = new RaycastInput
            {
                Start = unityRay.origin,
                End = unityRay.origin + unityRay.direction * MousePickSystem.k_MaxDistance,
                Filter = CollisionFilter.Default,
            };

            var mouseHoverEntity = GetSingletonEntity<MouseHover>();
            var mouseHover = EntityManager.GetSharedComponentData<MouseHover>(mouseHoverEntity);

            RaycastHit hit;
            using (var raycastHitRef = new NativeReference<RaycastHit>(Allocator.TempJob))
            {
                var rcj = new WorldRaycastJob()
                {
                    CollisionWorld = collisionWorld,
                    RayInput = rayInput,
                    IgnoreTriggers = mouseHover.IgnoreTriggers,
                    IgnoreStatic = mouseHover.IgnoreStatic,
                    RaycastHitRef = raycastHitRef
                };
                rcj.Run();
                hit = raycastHitRef.Value;
            }

            var graphicsEntity = FindGraphicsEntityFromPhysics(hit.Entity, hit.ColliderKey);

            // If still hovering over the same entity then do nothing.
            if (mouseHover.CurrentEntity.Equals(graphicsEntity)) return;

            mouseHover.PreviousEntity = mouseHover.CurrentEntity;
            mouseHover.CurrentEntity = graphicsEntity;

            bool hasPreviousEntity = !mouseHover.PreviousEntity.Equals(Entity.Null);
            bool hasCurrentEntity = !mouseHover.CurrentEntity.Equals(Entity.Null);

            // If there was a previous entity and it had a RenderMesh then reset its Material
            if (hasPreviousEntity && EntityManager.HasComponent<RenderMesh>(mouseHover.PreviousEntity))
            {
                var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(mouseHover.PreviousEntity);
                renderMesh.material = mouseHover.OriginalMaterial;
                EntityManager.SetSharedComponentData(mouseHover.PreviousEntity, renderMesh);
            }

            // If there was a new current entity and it has a RenderMesh then set its Material
            if (hasCurrentEntity && EntityManager.HasComponent<RenderMesh>(mouseHover.CurrentEntity))
            {
                var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(mouseHover.CurrentEntity);
                mouseHover.OriginalMaterial = renderMesh.material;
                mouseHover.PreviousEntity = mouseHover.CurrentEntity;
                mouseHover.CurrentEntity = graphicsEntity;
                renderMesh.material = mouseHover.HoverMaterial;
                EntityManager.SetSharedComponentData(mouseHover.CurrentEntity, renderMesh);
            }

            EntityManager.SetSharedComponentData(mouseHoverEntity, mouseHover);
        }
    }
}
