using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Physics.Tests.Authoring
{
    class BodyShapeAndShapeConversionSystemsTransformSystemsIntegrationTests : BaseHierarchyConversionTest
    {
        [TestCaseSource(nameof(k_ExplicitPhysicsBodyHierarchyTestCases))]
        public void ConversionSystems_WhenChildGOHasExplicitPhysicsBody_EntityIsInExpectedHierarchyLocation(
            BodyMotionType motionType, EntityQueryDesc expectedQuery
        )
        {
            CreateHierarchy(
                new[] { typeof(PhysicsPreserveTransformAuthoring) },
                new[] { typeof(PhysicsPreserveTransformAuthoring) },
                new[] { typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring) }
            );
            Child.GetComponent<PhysicsBodyAuthoring>().MotionType = motionType;

            TransformConversionUtils.ConvertHierarchyAndUpdateTransformSystemsVerifyEntityExists(Root, expectedQuery);
        }

        [Test]
        public void ConversionSystems_WhenChildGOHasImplicitStaticBody_EntityIsInHierarchy(
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type colliderType
        )
        {
            CreateHierarchy(
                new[] { typeof(PhysicsPreserveTransformAuthoring) },
                new[] { typeof(PhysicsPreserveTransformAuthoring) },
                new[] { colliderType }
            );

            var query = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(PhysicsCollider), typeof(Parent) }
            };
            TransformConversionUtils.ConvertHierarchyAndUpdateTransformSystemsVerifyEntityExists(Root, query);
        }

        [Test]
        public void ConversionSystems_WhenGOHasPhysicsComponents_EntityHasSameLocalToWorldAsGO(
            [Values(
                typeof(Rigidbody),
                typeof(PhysicsBodyAuthoring), null
             )]
            Type bodyType,
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type colliderType,
            [Values(typeof(StaticOptimizeEntity), null)] Type otherType
        )
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                Array.Empty<Type>(),
                new[] { bodyType, colliderType, otherType }.Where(t => t != null).ToArray()
            );
            TransformHierarchyNodes();

            var localToWorld = TransformConversionUtils.ConvertHierarchyAndUpdateTransformSystems<LocalToWorld>(Root);

            Assert.That(localToWorld.Value, Is.PrettyCloseTo(Child.transform.localToWorldMatrix));
        }

        [Test]
        public void ConversionSystems_WhenGOIsImplicitStaticBody_EntityHasSameLocalToWorldAsGO(
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type colliderType
        )
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                Array.Empty<Type>(),
                new[] { colliderType }
            );
            TransformHierarchyNodes();

            var localToWorld = TransformConversionUtils.ConvertHierarchyAndUpdateTransformSystems<LocalToWorld>(Root);

            Assert.That(localToWorld.Value, Is.PrettyCloseTo(Child.transform.localToWorldMatrix));
        }

        [Test]
        public void ConversionSystems_WhenGOHasNonStaticBody_EntityHasRotationInWorldSpace(
            [Values(
                typeof(Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type bodyType,
            [Values(BodyMotionType.Dynamic, BodyMotionType.Kinematic)]
            BodyMotionType motionType,
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type colliderType
        )
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                Array.Empty<Type>(),
                new[] { bodyType, colliderType }.Where(t => t != null).ToArray()
            );
            TransformHierarchyNodes();
            SetBodyMotionType(Child.GetComponent(bodyType), motionType);

            var rotation = TransformConversionUtils.ConvertHierarchyAndUpdateTransformSystems<LocalTransform>(Root).Rotation;

            var expectedRotation = Math.DecomposeRigidBodyOrientation(Child.transform.localToWorldMatrix);
            Assert.That(rotation, Is.EqualTo(expectedRotation));
        }

        static void SetBodyMotionType(Component component, BodyMotionType motionType)
        {
            var rigidBody = component as Rigidbody;
            if (rigidBody != null)
            {
                rigidBody.isKinematic = motionType == BodyMotionType.Kinematic;
                return;
            }
            var physicsBody = component as PhysicsBodyAuthoring;
            physicsBody.MotionType = motionType;
        }

        [Test]
        public void ConversionSystems_WhenGOHasPhysicsComponents_EntityHasTranslationInWorldSpace(
            [Values(
                typeof(Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type bodyType,
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type colliderType,
            [Values(typeof(StaticOptimizeEntity), null)]
            Type otherType
        )
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                Array.Empty<Type>(),
                new[] { bodyType, colliderType, otherType }.Where(t => t != null).ToArray()
            );
            TransformHierarchyNodes();

            var translation = TransformConversionUtils.ConvertHierarchyAndUpdateTransformSystems<LocalTransform>(Root);
            Assert.That(translation.Position, Is.PrettyCloseTo(Child.transform.position));
        }

        [Test, Description("Validate that GameObject.isStatic and StaticOptimizeEntity are baked to equivalent results.")]
        public void ConversionSystems_StaticOptimizeRoot_IsEquivalent_StaticRoot(
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type colliderType
        )
        {
            void TransformHierarchyNodes(GameObject root, GameObject parent, GameObject child)
            {
                root.transform.localPosition = new Vector3(1f, 2f, 3f);
                root.transform.localRotation = Quaternion.Euler(30f, 60f, 90f);
                root.transform.localScale = new Vector3(3f, 5f, 7f);
                parent.transform.localPosition = new Vector3(2f, 4f, 8f);
                parent.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
                parent.transform.localScale = new Vector3(2f, 4f, 8f);
                child.transform.localPosition = new Vector3(3f, 6f, 9f);
                child.transform.localRotation = Quaternion.Euler(15f, 30f, 45f);
                child.transform.localScale = new Vector3(-1f, 2f, -4f);
            }

            var staticRoot = new GameObject("Root");
            var staticParent = new GameObject("Parent");
            var staticChild = new GameObject("Child", colliderType);
            staticChild.transform.parent = staticParent.transform;
            staticParent.transform.parent = staticRoot.transform;
            staticRoot.isStatic = true;

            var staticOptRoot = new GameObject("Root", typeof(StaticOptimizeEntity));
            var staticOptParent = new GameObject("Parent");
            var staticOptChild = new GameObject("Child", colliderType);
            staticOptChild.transform.parent = staticOptParent.transform;
            staticOptParent.transform.parent = staticOptRoot.transform;

            try
            {
                TransformHierarchyNodes(staticRoot, staticParent, staticChild);
                TransformHierarchyNodes(staticOptRoot, staticOptParent, staticOptChild);

                using (var world = new World("Test world"))
                using (var blobAssetStore = new BlobAssetStore(128))
                {
                    var staticRootEntity = ConvertBakeGameObject(staticRoot, world, blobAssetStore);
                    var staticOptRootEntity = ConvertBakeGameObject(staticRoot, world, blobAssetStore);

                    var query = new EntityQueryBuilder(Allocator.Temp).WithAll<PhysicsCollider>().Build(world.EntityManager);
                    Assert.AreEqual(2, query.CalculateEntityCount(), "Unexpected number of static roots");
                    var roots = query.ToEntityArray(Allocator.Temp).ToArray();
                    Assert.Contains(staticRootEntity, roots, "Root with static flag was not baked");
                    Assert.Contains(staticOptRootEntity, roots, "Root with StaticOptimizeEntity was not baked");
                    Assert.AreEqual(world.EntityManager.GetComponentCount(staticOptRootEntity), world.EntityManager.GetComponentCount(staticRootEntity), "The static roots have different numbers of components");
                }
            }
            finally
            {
                GameObject.DestroyImmediate(staticRoot);
                GameObject.DestroyImmediate(staticParent);
                GameObject.DestroyImmediate(staticChild);
                GameObject.DestroyImmediate(staticOptRoot);
                GameObject.DestroyImmediate(staticOptParent);
                GameObject.DestroyImmediate(staticOptChild);
            }
        }
    }
}
