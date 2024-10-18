using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;
#if !UNITY_EDITOR
using UnityEngine.TestTools;
#endif

namespace Unity.Physics.Tests.Authoring
{
    class PhysicsShapeConversionSystemIntegrationTests : BaseHierarchyConversionTest
    {
        private Mesh NonReadableMesh { get; set; }
        private Mesh ReadableMesh { get; set; }
        private Mesh MeshWithMultipleSubMeshes { get; set; }

        private Mesh TrivialMesh { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ReadableMesh = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
            Assume.That(ReadableMesh.isReadable, Is.True, $"{ReadableMesh} is not readable.");

            NonReadableMesh = Mesh.Instantiate(ReadableMesh);
            NonReadableMesh.UploadMeshData(true);
            Assume.That(NonReadableMesh.isReadable, Is.False, $"{NonReadableMesh} is readable.");

            MeshWithMultipleSubMeshes = new Mesh
            {
                name = nameof(MeshWithMultipleSubMeshes),
                vertices = new[]
                {
                    new Vector3(0f, 1f, 0f),
                    new Vector3(1f, 1f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 0f, 0f)
                },
                normals = new[]
                {
                    Vector3.back,
                    Vector3.back,
                    Vector3.back,
                    Vector3.back
                },
                subMeshCount = 2
            };
            MeshWithMultipleSubMeshes.SetTriangles(new[] { 0, 1, 2 }, 0);
            MeshWithMultipleSubMeshes.SetTriangles(new[] { 2, 3, 0 }, 1);
            Assume.That(MeshWithMultipleSubMeshes.isReadable, Is.True, $"{MeshWithMultipleSubMeshes} is not readable.");

            TrivialMesh = new Mesh()
            {
                vertices = new[]
                {
                    new Vector3(1f, 1f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 0f, 0f)
                },
                normals = new[]
                {
                    Vector3.back,
                    Vector3.back,
                    Vector3.back
                },
                triangles = new[]
                {
                    0, 1, 2
                },
                subMeshCount = 1
            };
            Assume.That(TrivialMesh.isReadable, Is.True, $"{TrivialMesh} is not readable.");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (NonReadableMesh != null)
                Mesh.DestroyImmediate(NonReadableMesh);
            if (MeshWithMultipleSubMeshes != null)
                Mesh.DestroyImmediate(MeshWithMultipleSubMeshes);
            if (TrivialMesh != null)
                Mesh.DestroyImmediate(TrivialMesh);
        }

        [Test]
        public void PhysicsShapeConversionSystem_WhenBodyHasOneSiblingShape_CreatesPrimitive()
        {
            CreateHierarchy(
                new[] { typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring) },
                Array.Empty<Type>(),
                Array.Empty<Type>()
            );
            Root.GetComponent<PhysicsShapeAuthoring>().SetBox(new BoxGeometry { Size = 1f, Orientation = quaternion.identity });

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(c => Assert.That(c.Value.Value.Type, Is.EqualTo(ColliderType.Box)), k_DefaultWorldIndex);
        }

        [Test]
        public void PhysicsShapeConversionSystem_WhenBodyHasOneDescendentShape_CreatesCompound()
        {
            CreateHierarchy(
                new[] { typeof(PhysicsBodyAuthoring) },
                new[] { typeof(PhysicsShapeAuthoring) },
                Array.Empty<Type>()
            );
            Parent.GetComponent<PhysicsShapeAuthoring>().SetBox(new BoxGeometry { Size = 1f, Orientation = quaternion.identity });

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(
                c =>
                {
                    Assert.That(c.Value.Value.Type, Is.EqualTo(ColliderType.Compound));
                    unsafe
                    {
                        var compoundCollider = (CompoundCollider*)c.Value.GetUnsafePtr();
                        Assert.That(compoundCollider->Children, Has.Length.EqualTo(1));
                        Assert.That(compoundCollider->Children[0].Collider->Type, Is.EqualTo(ColliderType.Box));
                    }
                },
                k_DefaultWorldIndex
            );
        }

        [Test]
        public void PhysicsShapeConversionSystem_WhenBodyHasOneDescendentShape_CreatesCompoundWithFiniteMass()
        {
            CreateHierarchy(
                new[] { typeof(PhysicsBodyAuthoring) },
                new[] { typeof(PhysicsShapeAuthoring) },
                Array.Empty<Type>()
            );
            Parent.GetComponent<PhysicsShapeAuthoring>().SetBox(new BoxGeometry { Size = 1f, Orientation = quaternion.identity });
            Parent.GetComponent<PhysicsShapeAuthoring>().CollisionResponse = CollisionResponsePolicy.RaiseTriggerEvents;

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(
                c =>
                {
                    Assert.That(c.Value.Value.Type, Is.EqualTo(ColliderType.Compound));
                    unsafe
                    {
                        var compoundCollider = (CompoundCollider*)c.Value.GetUnsafePtr();
                        Assume.That(compoundCollider->Children, Has.Length.EqualTo(1));
                        Assume.That(compoundCollider->Children[0].Collider->Type, Is.EqualTo(ColliderType.Box));

                        // Make sure compound mass properties are calculated properly
                        Assert.That(compoundCollider->MassProperties.Volume > 0.0f);
                        Assert.That(math.all(math.isfinite(compoundCollider->MassProperties.MassDistribution.Transform.pos)));
                        Assert.That(math.all(math.isfinite(compoundCollider->MassProperties.MassDistribution.Transform.rot.value)));
                        Assert.That(math.all(math.isfinite(compoundCollider->MassProperties.MassDistribution.InertiaTensor)));
                        Assert.That(math.all(math.isfinite(compoundCollider->MassProperties.MassDistribution.InertiaMatrix.c0)));
                        Assert.That(math.all(math.isfinite(compoundCollider->MassProperties.MassDistribution.InertiaMatrix.c1)));
                        Assert.That(math.all(math.isfinite(compoundCollider->MassProperties.MassDistribution.InertiaMatrix.c2)));
                    }
                },
                k_DefaultWorldIndex
            );
        }

        [Test]
        public void PhysicsShapeConversionSystem_WhenBodyHasMultipleDescendentShapes_CreatesCompound()
        {
            CreateHierarchy(
                new[] { typeof(PhysicsBodyAuthoring) },
                new[] { typeof(PhysicsShapeAuthoring) },
                new[] { typeof(PhysicsShapeAuthoring) }
            );
            Parent.GetComponent<PhysicsShapeAuthoring>().SetBox(new BoxGeometry { Size = 1f, Orientation = quaternion.identity });
            Child.GetComponent<PhysicsShapeAuthoring>().SetSphere(new SphereGeometry { Radius = 1f }, quaternion.identity);

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(
                c =>
                {
                    Assert.That(c.Value.Value.Type, Is.EqualTo(ColliderType.Compound));
                    unsafe
                    {
                        var compoundCollider = (CompoundCollider*)c.Value.GetUnsafePtr();

                        var childTypes = Enumerable.Range(0, compoundCollider->NumChildren)
                            .Select(i => compoundCollider->Children[i].Collider->Type)
                            .ToArray();
                        Assert.That(childTypes, Is.EquivalentTo(new[] { ColliderType.Box, ColliderType.Sphere }));
                    }
                },
                k_DefaultWorldIndex
            );
        }

        [Test]
        public void PhysicsShapeConversionSystem_WhenShapeHasNonReadableConvex_ThrowsException()
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { typeof(PhysicsShapeAuthoring) });
            Child.GetComponent<PhysicsShapeAuthoring>().SetConvexHull(default, NonReadableMesh);

            VerifyLogsException<InvalidOperationException>(k_NonReadableMeshPattern);
        }

        [Test]
        public void PhysicsShapeConversionSystem_WhenShapeHasNonReadableMesh_ThrowsException()
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { typeof(PhysicsShapeAuthoring) });
            Child.GetComponent<PhysicsShapeAuthoring>().SetMesh(NonReadableMesh);

            VerifyLogsException<InvalidOperationException>(k_NonReadableMeshPattern);
        }

        [Test]
        public void PhysicsShapeConversionSystems_WhenMeshCollider_MultipleSubMeshes_AllSubMeshesIncluded(
            [Values(
                typeof(UnityEngine.MeshCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type shapeType
        )
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { shapeType });
            if (Child.GetComponent(shapeType) is UnityEngine.MeshCollider meshCollider)
                meshCollider.sharedMesh = MeshWithMultipleSubMeshes;
            else
                Child.GetComponent<PhysicsShapeAuthoring>().SetMesh(MeshWithMultipleSubMeshes);

            TestMeshData(numExpectedMeshSections: 1, numExpectedPrimitivesPerSection: new[] {1}, quadPrimitiveExpectedFlags: new[] {new[] {true}});
        }

        [Test]
        public void PhysicsShapeConversionSystems_WhenGOHasShape_GOIsActive_AuthoringComponentEnabled_AuthoringDataConverted(
            [Values(
                typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.SphereCollider), typeof(UnityEngine.MeshCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type shapeType
        )
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { shapeType });
            if (Child.GetComponent(shapeType) is UnityEngine.MeshCollider meshCollider)
                meshCollider.sharedMesh = ReadableMesh;

            // conversion presumed to create valid PhysicsCollider under default conditions
            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(c => Assert.That(c.IsValid, Is.True), k_DefaultWorldIndex);
        }

        [Test]
        public void PhysicsShapeConversionSystems_WhenGOHasShape_AuthoringComponentDisabled_AuthoringDataNotConverted(
            [Values(
                typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.SphereCollider), typeof(UnityEngine.MeshCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type shapeType
        )
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { shapeType });
            if (Child.GetComponent(shapeType) is UnityEngine.MeshCollider meshCollider)
                meshCollider.sharedMesh = ReadableMesh;
            var c = Child.GetComponent(shapeType);
            if (c is UnityEngine.Collider collider)
                collider.enabled = false;
            else
                (c as PhysicsShapeAuthoring).enabled = false;

            // conversion presumed to create valid PhysicsCollider under default conditions
            // covered by corresponding test ConversionSystems_WhenGOHasShape_GOIsActive_AuthoringComponentEnabled_AuthoringDataConverted
            VerifyNoDataProduced<PhysicsCollider>();
        }

        [Test]
        public void PhysicsShapeConversionSystems_WhenGOHasShape_GOIsInactive_BodyIsNotConverted(
            [Values] Node inactiveNode,
            [Values(
                typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.SphereCollider), typeof(UnityEngine.MeshCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type shapeType
        )
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { shapeType });
            if (Child.GetComponent(shapeType) is UnityEngine.MeshCollider meshCollider)
                meshCollider.sharedMesh = ReadableMesh;

            GetNode(inactiveNode).SetActive(false);
            var numInactiveNodes = Root.GetComponentsInChildren<Transform>(true).Count(t => t.gameObject.activeSelf);
            Assume.That(numInactiveNodes, Is.EqualTo(2));

            // conversion presumed to create valid PhysicsCollider under default conditions
            // covered by corresponding test ConversionSystems_WhenGOHasShape_GOIsActive_AuthoringComponentEnabled_AuthoringDataConverted
            VerifyNoDataProduced<PhysicsCollider>();
        }

        static void SetDefaultShape(PhysicsShapeAuthoring shape, ShapeType type)
        {
            switch (type)
            {
                case ShapeType.Box:
                    shape.SetBox(default);
                    break;
                case ShapeType.Capsule:
                    shape.SetCapsule(new CapsuleGeometryAuthoring { OrientationEuler = EulerAngles.Default });
                    break;
                case ShapeType.Sphere:
                    shape.SetSphere(default, quaternion.identity);
                    break;
                case ShapeType.Cylinder:
                    shape.SetCylinder(new CylinderGeometry { SideCount = CylinderGeometry.MaxSideCount });
                    break;
                case ShapeType.Plane:
                    shape.SetPlane(default, default, quaternion.identity);
                    break;
                case ShapeType.ConvexHull:
                    shape.SetConvexHull(ConvexHullGenerationParameters.Default);
                    break;
                case ShapeType.Mesh:
                    shape.SetMesh();
                    break;
            }

            shape.FitToEnabledRenderMeshes();
        }

        [Test]
        public unsafe void PhysicsShapeConversionSystems_WhenMultipleShapesShareInputs_CollidersShareTheSameData(
            [Values(ShapeType.ConvexHull, ShapeType.Mesh)] ShapeType shapeType
        )
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring), typeof(MeshFilter), typeof(MeshRenderer) },
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring), typeof(MeshFilter), typeof(MeshRenderer) }
            );
            foreach (var meshFilter in Root.GetComponentsInChildren<MeshFilter>())
                meshFilter.sharedMesh = ReadableMesh;
            foreach (var shape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
            {
                SetDefaultShape(shape, shapeType);
                shape.ForceUnique = false;
            }
            Child.transform.localPosition = TransformConversionUtils.k_SharedDataChildTransformation.pos;
            Child.transform.localRotation = TransformConversionUtils.k_SharedDataChildTransformation.rot;

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(colliders =>
            {
                var uniqueColliders = new HashSet<int>();
                foreach (var c in colliders)
                    uniqueColliders.Add((int)c.ColliderPtr);
                var numUnique = uniqueColliders.Count;
                Assert.That(numUnique, Is.EqualTo(1), $"Expected colliders to reference the same data, but found {numUnique} different colliders.");
            }, 2, k_DefaultWorldIndex);
        }

        static readonly TestCaseData[] k_MultipleAuthoringComponentsTestCases =
        {
            new TestCaseData(
                new[] { typeof(Rigidbody), typeof(UnityEngine.BoxCollider), typeof(UnityEngine.BoxCollider) },
                Array.Empty<Type>(),
                new[] { ColliderType.Box, ColliderType.Box }
            ).SetName("PhysicsShapeConversionSystems_WhenRigidbodyHasMultipleBoxColliders_CreatesCompound"),
            new TestCaseData(
                new[] { typeof(PhysicsBodyAuthoring), typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.SphereCollider), typeof(PhysicsShapeAuthoring) },
                Array.Empty<Type>(),
                new[] { ColliderType.Box, ColliderType.Box, ColliderType.Capsule, ColliderType.Sphere }
            ).SetName("PhysicsShapeConversionSystems_WhenPhysicsBodyHasMixedColliders_CreatesCompound"),
            new TestCaseData(
                new[] { typeof(Rigidbody)},
                new[] { typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.SphereCollider) },
                new[] { ColliderType.Box, ColliderType.Capsule, ColliderType.Sphere }
            ).SetName("PhysicsShapeConversionSystems_WhenRigidbodyHasCollidersOnlyInDescendents_CreatesCompound"),
            new TestCaseData(
                new[] { typeof(Rigidbody), typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider)},
                new[] { typeof(UnityEngine.SphereCollider), typeof(UnityEngine.BoxCollider) },
                new[] { ColliderType.Box, ColliderType.Box, ColliderType.Capsule, ColliderType.Sphere }
            ).SetName("PhysicsShapeConversionSystems_WhenRigidbodyHasCollidersAlsoInDescendents_CreatesCompound"),
        };

        [TestCaseSource(nameof(k_MultipleAuthoringComponentsTestCases))]
        public void PhysicsShapeConversionSystems_CompoundColliderCreation(
            Type[] rootComponentTypes, Type[] parentComponentTypes, ColliderType[] expectedColliderTypes
        )
        {
            CreateHierarchy(rootComponentTypes, parentComponentTypes, Array.Empty<Type>());
            Root.GetComponent<PhysicsShapeAuthoring>()?.SetBox(new BoxGeometry { Size = 1f });

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(
                (w, e, c) =>
                {
                    Assert.That(c.Value.Value.Type, Is.EqualTo(ColliderType.Compound));
                    unsafe
                    {
                        var compoundCollider = (CompoundCollider*)c.Value.GetUnsafePtr();

                        var childTypes = Enumerable.Range(0, compoundCollider->NumChildren)
                            .Select(i => compoundCollider->Children[i].Collider->Type)
                            .ToArray();
                        Assert.That(childTypes, Is.EquivalentTo(expectedColliderTypes));

                        // make sure we have a collider key entity pair buffer with the right size
                        Assert.That(w.EntityManager.HasBuffer<PhysicsColliderKeyEntityPair>(e), Is.True);
                        var buffer = w.EntityManager.GetBuffer<PhysicsColliderKeyEntityPair>(e);
                        Assert.That(buffer.Length, Is.EqualTo(compoundCollider->NumChildren));

                        // make sure the content of the buffer is correct
                        for (int i = 0; i < buffer.Length; ++i)
                        {
                            var bufferElement = buffer[i];

                            // make sure the referenced entity exists
                            Assert.That(w.EntityManager.Exists(bufferElement.Entity), Is.True);

                            // make sure the collider key works, that is, we have a valid child collider for each key in the buffer
                            Assert.IsTrue(compoundCollider->GetChild(ref bufferElement.Key, out var childLookup));

                            // Make sure the entity is correct.
                            // Note: we expect it to be set to Null within the collider blob because in the blob the entity
                            // can not be automatically updated when its internal ID changes, as opposed to when it appears in a component or buffer
                            // such as the PhysicsColliderKeyEntityPair. It is set to Entity.Null to avoid having an invalid entity reference.
                            // It can still be used as user data for user-created compound colliders.
                            var childInCompound = compoundCollider->Children[i];
                            Assert.That(Entity.Null, Is.EqualTo(childInCompound.Entity));
                            Assert.That(Entity.Null, Is.EqualTo(childLookup.Entity));
                        }
                    }
                },
                k_DefaultWorldIndex
            );
        }

        [Test]
        public unsafe void PhysicsShapeConversionSystems_WhenMultipleShapesShareMeshes_CollidersShareTheSameData(
            [Values(ShapeType.ConvexHull, ShapeType.Mesh)] ShapeType shapeType
        )
        {
            CreateHierarchy(
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring) },
                new[] { typeof(MeshFilter), typeof(MeshRenderer) },
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring), typeof(MeshFilter), typeof(MeshRenderer) }
            );

            foreach (var meshFilter in Root.GetComponentsInChildren<MeshFilter>())
                meshFilter.sharedMesh = MeshWithMultipleSubMeshes;
            foreach (var shape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
            {
                SetDefaultShape(shape, shapeType);
                shape.ForceUnique = false;
            }

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(colliders =>
            {
                var uniqueColliders = new HashSet<int>();
                foreach (var c in colliders)
                    uniqueColliders.Add((int)c.ColliderPtr);
                var numUnique = uniqueColliders.Count;
                Assert.That(numUnique, Is.EqualTo(1), $"Expected colliders to reference unique data, but found {numUnique} different colliders.");
            }, 2, k_DefaultWorldIndex);
        }

        [Test]
        public unsafe void PhysicsShapeConversionSystems_WhenMultipleShapesShareMeshes_WithDifferentOffsets_CollidersDoNotShareTheSameData(
            [Values(ShapeType.ConvexHull, ShapeType.Mesh)] ShapeType shapeType
        )
        {
            CreateHierarchy(
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring) },
                new[] { typeof(MeshFilter), typeof(MeshRenderer) },
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring), typeof(MeshFilter), typeof(MeshRenderer) }
            );
            foreach (var meshFilter in Root.GetComponentsInChildren<MeshFilter>())
                meshFilter.sharedMesh = ReadableMesh;
            foreach (var shape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
            {
                SetDefaultShape(shape, shapeType);
                shape.ForceUnique = false;
            }
            // Root will get mesh from Parent (with offset) and Child will get mesh from itself (no offset)
            Parent.transform.localPosition = TransformConversionUtils.k_SharedDataChildTransformation.pos;
            Parent.transform.localRotation = TransformConversionUtils.k_SharedDataChildTransformation.rot;

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(colliders =>
            {
                var uniqueColliders = new HashSet<int>();
                foreach (var c in colliders)
                    uniqueColliders.Add((int)c.ColliderPtr);
                var numUnique = uniqueColliders.Count;
                Assert.That(numUnique, Is.EqualTo(2), $"Expected colliders to reference unique data, but found {numUnique} different colliders.");
            }, 2, k_DefaultWorldIndex);
        }

        [Test]
        public unsafe void PhysicsShapeConversionSystems_WhenMultipleShapesShareMeshes_WithDifferentInheritedScale_CollidersDontShareTheSameData_IfNonUniformScale(
            [Values(ShapeType.ConvexHull, ShapeType.Mesh)] ShapeType shapeType, [Values] bool uniformScale
        )
        {
            CreateHierarchy(
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring), typeof(MeshFilter), typeof(MeshRenderer) },
                Array.Empty<Type>(),
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring), typeof(MeshFilter), typeof(MeshRenderer) }
            );
            foreach (var meshFilter in Root.GetComponentsInChildren<MeshFilter>())
                meshFilter.sharedMesh = ReadableMesh;
            foreach (var shape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
            {
                SetDefaultShape(shape, shapeType);
                shape.ForceUnique = false;
            }

            // Modify scale of one collider. Note that the collider geometry will not be affected if the scale is uniform.
            // In this case we expect the LocalTransform.Scale to contain the provided scale value.

            const float kScale = 2f;
            if (uniformScale)
            {
                Parent.transform.localScale = new float3(kScale);
            }
            else
            {
                Parent.transform.localScale = new float3(kScale, 1, 1);
            }

            var expectedUniqueColliders = uniformScale ? 1 : 2;
            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>((world, entities, colliders) =>
            {
                // make sure we have the expected number of uniformly scaled colliders
                int foundUniformScaleCount = 0;
                foreach (var e in entities)
                {
                    // expect the LocalTransform.Scale to be set correctly
                    var localTransform = world.EntityManager.GetComponentData<LocalTransform>(e);
                    foundUniformScaleCount += math.abs(localTransform.Scale - kScale) < 1e-5 ? 1 : 0;
                }
                Assert.That(foundUniformScaleCount, Is.EqualTo(uniformScale ? 1 : 0));

                // make sure we have the expected number of unique colliders
                var uniqueColliders = new HashSet<IntPtr>();
                foreach (var c in colliders)
                {
                    uniqueColliders.Add((IntPtr)c.ColliderPtr);
                }
                var numUnique = uniqueColliders.Count;

                Assert.That(numUnique, Is.EqualTo(expectedUniqueColliders), $"Expected {expectedUniqueColliders} unique colliders, but found {numUnique} unique colliders.");
            }, 2, k_DefaultWorldIndex);
        }

        [Test]
        public unsafe void PhysicsShapeConversionSystems_WhenMultipleShapesShareInputs_AndShapeIsForcedUnique_CollidersDoNotShareTheSameData(
            [Values] ShapeType shapeType
        )
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring), typeof(MeshFilter), typeof(MeshRenderer) },
                new[] { typeof(PhysicsShapeAuthoring), typeof(PhysicsBodyAuthoring), typeof(MeshFilter), typeof(MeshRenderer) }
            );
            foreach (var meshFilter in Root.GetComponentsInChildren<MeshFilter>())
                meshFilter.sharedMesh = ReadableMesh;
            foreach (var shape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
            {
                SetDefaultShape(shape, shapeType);
                shape.ForceUnique = true;
            }

            TestConvertedSharedData<PhysicsCollider, PhysicsWorldIndex>(colliders =>
            {
                var uniqueColliders = new HashSet<int>();
                foreach (var c in colliders)
                    uniqueColliders.Add((int)c.ColliderPtr);

                var numUnique = uniqueColliders.Count;
                Assert.That(numUnique, Is.EqualTo(2), $"Expected colliders to reference unique data, but found {numUnique} different colliders.");
            }, 2, k_DefaultWorldIndex);
        }

        struct TriangleCounter : ILeafColliderCollector
        {
            public int NumTriangles;

            public unsafe void AddLeaf(ColliderKey key, ref ChildCollider leaf)
            {
                var collider = leaf.Collider;
                if (collider->Type == ColliderType.Triangle)
                {
                    ++NumTriangles;
                }
            }

            public void PushCompositeCollider(ColliderKeyPath compositeKey, Math.MTransform parentFromComposite, out Math.MTransform worldFromParent)
            {
                worldFromParent = new Math.MTransform();

                // does nothing
            }

            public void PopCompositeCollider(uint numCompositeKeyBits, Math.MTransform worldFromParent)
            {
                // does nothing
            }
        }

        [Test]
        public void PhysicsShapeConversionSystems_WhenNoCustomMeshSpecified_ChildMeshesAreIncluded()
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                new[] {typeof(PhysicsShapeAuthoring), typeof(MeshFilter)},
                new[] {typeof(MeshFilter)}
            );
            var meshFilter = Parent.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = TrivialMesh;
            var childMeshFilter = Child.GetComponent<MeshFilter>();
            childMeshFilter.sharedMesh = TrivialMesh;
            Child.transform.position += new Vector3(42, 42, 42);

            var shape = Parent.GetComponent<PhysicsShapeAuthoring>();
            shape.SetMesh();

            var expectedTriangleCount = (TrivialMesh.triangles.Length / 3) * 2;

            TestConvertedData<PhysicsCollider>(collider =>
            {
                unsafe
                {
                    Assert.That(collider.Value.Value.Type, Is.EqualTo(ColliderType.Mesh));
                    var meshCollider = (MeshCollider*)collider.ColliderPtr;
                    var triangleCounter = new TriangleCounter();
                    meshCollider->GetLeaves(ref triangleCounter);

                    Assert.That(triangleCounter.NumTriangles, Is.EqualTo(expectedTriangleCount));
                }
            });
        }

        [Test]
        public void PhysicsShapeConversionSystems_WhenCustomMeshSpecified_ChildMeshesAreIgnored()
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                new[] {typeof(PhysicsShapeAuthoring), typeof(MeshFilter)},
                new[] {typeof(MeshFilter)}
            );
            var meshFilter = Parent.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = ReadableMesh;
            var childMeshFilter = Child.GetComponent<MeshFilter>();
            childMeshFilter.sharedMesh = ReadableMesh;
            Child.transform.position += new Vector3(42, 42, 42);

            Assert.That(ReadableMesh.triangles.Length / 3, Is.GreaterThan(0));

            var shape = Parent.GetComponent<PhysicsShapeAuthoring>();
            shape.SetMesh(TrivialMesh);
            var expectedTriangleCount = TrivialMesh.triangles.Length / 3;

            TestConvertedData<PhysicsCollider>(collider =>
            {
                unsafe
                {
                    Assert.That(collider.Value.Value.Type, Is.EqualTo(ColliderType.Mesh));
                    var meshCollider = (MeshCollider*)collider.ColliderPtr;
                    var triangleCounter = new TriangleCounter();
                    meshCollider->GetLeaves(ref triangleCounter);

                    Assert.That(triangleCounter.NumTriangles, Is.EqualTo(expectedTriangleCount));
                }
            });
        }

        void CreateHierarchyWithChildShape(ShapeType shapeType)
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                Array.Empty<Type>(),
                new[] { typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring), typeof(MeshFilter), typeof(MeshRenderer) }
            );

            // Set mesh in mesh filter so that we can get a default size for all shapes via auto-fitting in SetDefaultShape()
            Child.GetComponent<MeshFilter>().sharedMesh = ReadableMesh;

            var physicsShape = Child.GetComponent<PhysicsShapeAuthoring>();
            SetDefaultShape(physicsShape, shapeType);
        }

        /// <summary>
        /// Test that when game object contains uniform scale, the resultant entity's local transform has the expected scale and the
        /// baked collider geometry is not affected by the scale.
        /// </summary>
        [Test]
        public void PhysicsShapeConversionSystems_WhenGOIsUniformlyScaled_LocalTransformHasScale_ColliderIsNotScaled([Values] ShapeType shapeType)
        {
            CreateHierarchyWithChildShape(shapeType);

            // uniformly transform the child collider
            const float k_UniformScale = 2f;
            Child.transform.localScale = new float3(k_UniformScale);

            var shape = Child.GetComponent<PhysicsShapeAuthoring>();
            TestConvertedData<LocalTransform>((world, transform, entity) =>
            {
                Assert.That(transform.Scale, Is.PrettyCloseTo(k_UniformScale));

                // make sure baked collider geometry is not affected by the uniform scale
                switch (shapeType)
                {
                    case ShapeType.Box:
                        {
                            // compare shape's box properties with baked BoxCollider properties and expect them to be identical
                            var boxGeometry = shape.GetBoxProperties();

                            var physicsCollider = world.EntityManager.GetComponentData<PhysicsCollider>(entity);
                            unsafe
                            {
                                var boxCollider = (BoxCollider*)physicsCollider.ColliderPtr;
                                // make sure the collider type is as expected
                                Assert.That(boxCollider->Type, Is.EqualTo(ColliderType.Box));

                                // compare box properties
                                Assert.That(boxCollider->Size, Is.PrettyCloseTo(boxGeometry.Size));
                                Assert.That(boxCollider->Center, Is.PrettyCloseTo(boxGeometry.Center));
                                Assert.That(boxCollider->Orientation, Is.OrientedEquivalentTo(boxGeometry.Orientation));
                            }

                            break;
                        }
                    case ShapeType.Capsule:
                        {
                            // compare shape's capsule properties with baked CapsuleCollider properties and expect them to be identical
                            var capsuleGeometry = shape.GetCapsuleProperties();
                            unsafe
                            {
                                var physicsCollider = world.EntityManager.GetComponentData<PhysicsCollider>(entity);
                                // make sure the collider type is as expected
                                Assert.That(physicsCollider.ColliderPtr->Type, Is.EqualTo(ColliderType.Capsule));

                                // compare capsule properties
                                var capsuleCollider = (CapsuleCollider*)physicsCollider.ColliderPtr;
                                var actualCenter = 0.5f * (capsuleCollider->Vertex0 + capsuleCollider->Vertex1);
                                var actualHeight = math.distance(capsuleCollider->Vertex0, capsuleCollider->Vertex1) + 2 * capsuleCollider->Radius;
                                var expectedDirection = new float3x3(capsuleGeometry.Orientation).c2;
                                var actualDirection = math.normalize(capsuleCollider->Vertex0 - capsuleCollider->Vertex1);
                                Assert.That(math.dot(actualDirection, expectedDirection), Is.PrettyCloseTo(1));
                                Assert.That(actualCenter, Is.PrettyCloseTo(capsuleGeometry.Center));
                                Assert.That(capsuleCollider->Radius, Is.PrettyCloseTo(capsuleGeometry.Radius));
                                Assert.That(actualHeight, Is.PrettyCloseTo(capsuleGeometry.Height));
                            }
                            break;
                        }
                    case ShapeType.Cylinder:
                        {
                            // compare shape's cylinder properties with baked CylinderCollider properties and expect them to be identical
                            var cylinderGeometry = shape.GetCylinderProperties();
                            unsafe
                            {
                                var physicsCollider = world.EntityManager.GetComponentData<PhysicsCollider>(entity);
                                // make sure the collider type is as expected
                                Assert.That(physicsCollider.ColliderPtr->Type, Is.EqualTo(ColliderType.Cylinder));

                                // compare cylinder properties
                                var cylinderCollider = (CylinderCollider*)physicsCollider.ColliderPtr;
                                Assert.That(cylinderCollider->Radius, Is.PrettyCloseTo(cylinderGeometry.Radius));
                                Assert.That(cylinderCollider->Height, Is.PrettyCloseTo(cylinderGeometry.Height));
                                Assert.That(cylinderCollider->SideCount, Is.EqualTo(cylinderGeometry.SideCount));
                                Assert.That(cylinderCollider->Center, Is.PrettyCloseTo(cylinderGeometry.Center));
                                Assert.That(cylinderCollider->Orientation, Is.OrientedEquivalentTo(cylinderGeometry.Orientation));
                            }
                            break;
                        }
                    case ShapeType.Sphere:
                        {
                            // compare shape's sphere properties with baked SphereCollider properties and expect them to be identical
                            var sphereGeometry = shape.GetSphereProperties(out quaternion orientation);
                            unsafe
                            {
                                var physicsCollider = world.EntityManager.GetComponentData<PhysicsCollider>(entity);
                                // make sure the collider type is as expected
                                Assert.That(physicsCollider.ColliderPtr->Type, Is.EqualTo(ColliderType.Sphere));

                                // compare sphere properties
                                var sphereCollider = (SphereCollider*)physicsCollider.ColliderPtr;
                                Assert.That(sphereCollider->Radius, Is.PrettyCloseTo(sphereGeometry.Radius));
                                Assert.That(sphereCollider->Center, Is.PrettyCloseTo(sphereGeometry.Center));
                            }
                            break;
                        }
                    case ShapeType.Plane:
                        {
                            // compare shape's plane properties with baked PolygonCollider properties and expect them to be identical
                            shape.GetPlaneProperties(out var center, out var size, out EulerAngles orientation);
                            PhysicsShapeExtensions.GetPlanePoints(center, size, orientation, out var vertex0, out var vertex1, out var vertex2, out var vertex3);

                            unsafe
                            {
                                var physicsCollider = world.EntityManager.GetComponentData<PhysicsCollider>(entity);
                                // make sure the collider type is as expected
                                Assert.That(physicsCollider.ColliderPtr->Type, Is.EqualTo(ColliderType.Quad));

                                // compare collider properties
                                var polygonCollider = (PolygonCollider*)physicsCollider.ColliderPtr;
                                Assert.That(polygonCollider->IsQuad);

                                Assert.That(polygonCollider->Vertices[0], Is.PrettyCloseTo(vertex0));
                                Assert.That(polygonCollider->Vertices[1], Is.PrettyCloseTo(vertex1));
                                Assert.That(polygonCollider->Vertices[2], Is.PrettyCloseTo(vertex2));
                                Assert.That(polygonCollider->Vertices[3], Is.PrettyCloseTo(vertex3));
                            }

                            break;
                        }
                    case ShapeType.Mesh:
                        {
                            // compare shape's mesh properties with baked MeshCollider properties and expect them to be unaffected by scale.
                            // Note: for simplicity we are using the mesh bounds here for comparison.

                            var expectedBounds = ReadableMesh.bounds;
                            var physicsCollider = world.EntityManager.GetComponentData<PhysicsCollider>(entity);
                            unsafe
                            {
                                var meshCollider = (MeshCollider*)physicsCollider.ColliderPtr;
                                // make sure the collider type is as expected
                                Assert.That(meshCollider->Type, Is.EqualTo(ColliderType.Mesh));

                                // compare  bounds
                                var actualBounds = meshCollider->CalculateAabb();
                                Assert.That(actualBounds.Center, Is.PrettyCloseTo(expectedBounds.center));
                                Assert.That(actualBounds.Extents, Is.PrettyCloseTo(expectedBounds.size));
                            }

                            break;
                        }
                    case ShapeType.ConvexHull:
                        {
                            // compare shape's convex hull properties with baked ConvexCollider properties and expect them to be unaffected by scale.
                            // Note: for simplicity we are using the mesh bounds here for comparison.

                            var expectedBounds = ReadableMesh.bounds;
                            var physicsCollider = world.EntityManager.GetComponentData<PhysicsCollider>(entity);
                            unsafe
                            {
                                var convexCollider = (ConvexCollider*)physicsCollider.ColliderPtr;
                                // make sure the collider type is as expected
                                Assert.That(convexCollider->Type, Is.EqualTo(ColliderType.Convex));

                                // compare bounds
                                var actualBounds = convexCollider->CalculateAabb();
                                Assert.That(actualBounds.Center, Is.PrettyCloseTo(expectedBounds.center));
                                Assert.That(actualBounds.Extents, Is.PrettyCloseTo(expectedBounds.size).Within(1e-2f));
                            }

                            break;
                        }
                }

                TestScaleChange(world, entity);
            });
        }

        /// <summary>
        /// Test that when game object contains non uniform scale, the resultant entity's local transform has identity scale and the
        /// PostTransformMatrix contains the non uniform scale.
        /// </summary>
        [Test]
        public void PhysicsShapeConversionSystem_WhenGOIsNonUniformlyScaled_LocalTransformHasNoScale(
            [Values] ShapeType shapeType)
        {
            CreateHierarchyWithChildShape(shapeType);

            // uniformly transform the child collider
            var k_NonUniformScale = new Vector3(1, 2, 3);
            Child.transform.localScale = k_NonUniformScale;

            TestConvertedData<LocalTransform>((world, transform, entity) =>
            {
                // expect the local transform scale to be identity
                Assert.That(transform.Scale, Is.PrettyCloseTo(1));

                // expect there to be a PostTransformMatrix component
                Assert.That(world.EntityManager.HasComponent<PostTransformMatrix>(entity), Is.True);

                // expect the PostTransformMatrix to represent the same scale as the local transform
                var postTransformMatrix = world.EntityManager.GetComponentData<PostTransformMatrix>(entity);
                Assert.That(postTransformMatrix.Value, Is.PrettyCloseTo(float4x4.Scale(k_NonUniformScale)));

                TestScaleChange(world, entity);
            });
        }

        /// <summary>
        /// Test that when a game object contains shear in world space, the resultant entity's local transform has identity scale and the
        /// PostTransformMatrix (containing the shear) and LocalTransform (containing the rigid body transform) components
        /// together represent the same world transform as the game object.
        /// </summary>
        [Test]
        public void PhysicsShapeConversionSystem_WhenGOIsSheared_LocalTransformHasNoScale(
            [Values] ShapeType shapeType)
        {
            CreateHierarchyWithChildShape(shapeType);

            // create a hierarchy that leads to shear in the child's world transform
            Root.transform.localPosition = new Vector3(1f, 2f, 3f);
            Root.transform.localRotation = Quaternion.Euler(30f, 60f, 90f);
            Root.transform.localScale = new Vector3(3f, 5f, 7f);
            Parent.transform.localPosition = new Vector3(2f, 4f, 8f);
            Parent.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
            Parent.transform.localScale = new Vector3(2f, 4f, 8f);
            Child.transform.localPosition = new Vector3(3f, 6f, 9f);
            Child.transform.localRotation = Quaternion.Euler(-30f, 20f, -10f);
            Child.transform.localScale = new Vector3(2f, 2f, 2f);

            var expectedColliderWorldTransform = (float4x4)Child.transform.localToWorldMatrix;
            Assert.That(expectedColliderWorldTransform.HasShear());

            TestConvertedData<LocalTransform>((world, transform, entity) =>
            {
                // expect the local transform scale to be identity
                var localTransform = transform;
                Assert.That(localTransform.Scale, Is.PrettyCloseTo(1));

                // expect there to be a PostTransformMatrix component
                Assert.That(world.EntityManager.HasComponent<PostTransformMatrix>(entity), Is.True);

                var postTransformMatrix = world.EntityManager.GetComponentData<PostTransformMatrix>(entity);
                // expect the post transform matrix to have shear
                Assert.That(postTransformMatrix.Value.HasShear());

                // check if world transform of the collider is as expected
                var actualColliderWorldTransform = math.mul(localTransform.ToMatrix(), postTransformMatrix.Value);
                Assert.That(expectedColliderWorldTransform, Is.PrettyCloseTo(actualColliderWorldTransform));

                TestScaleChange(world, entity);
            });
        }

        /// <summary>
        /// Test that when a game object contains non-uniform scale in world space, a box or mesh collider
        /// have the non-uniform scale baked in.
        /// </summary>
        [Test]
        public void PhysicsShapeConversionSystem_WhenGOIsNonUniformlyScaled_ColliderHasBakedScale(
            [Values(ShapeType.Box, ShapeType.Mesh)] ShapeType shapeType)
        {
            TestNonUniformScaleOnCollider(new[] {typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)}, gameObjectToConvert =>
            {
                // create a primitive cube which we will assign to the game object used in the test
                var cubeGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var cubeMeshFilter = cubeGameObject.GetComponent<MeshFilter>();
                var cubeMeshRenderer = cubeGameObject.GetComponent<MeshRenderer>();
                Assert.That(cubeMeshFilter != null && cubeMeshFilter.sharedMesh != null && cubeMeshRenderer != null);

                // set up the test game object with a mesh filter and renderer of a cube
                var meshFilter = gameObjectToConvert.GetComponent<MeshFilter>();
                meshFilter.mesh = cubeMeshFilter.sharedMesh;
                var meshRenderer = gameObjectToConvert.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = cubeMeshRenderer.sharedMaterial;

                var shape = Child.GetComponent<PhysicsShapeAuthoring>();
                if (shapeType == ShapeType.Mesh)
                {
                    shape.SetMesh(cubeMeshFilter.sharedMesh);
                }
                else if (shapeType == ShapeType.Box)
                {
                    SetDefaultShape(shape, ShapeType.Box);
                }
                else
                {
                    throw new NotImplementedException();
                }

                UnityEngine.Object.DestroyImmediate(cubeGameObject);
            });
        }

        /// <summary>
        /// Test that when a game object contains non-uniform scale in world space, a convex collider
        /// has the non-uniform scale baked in.
        /// </summary>
        [Test]
        public void PhysicsShapeConversionSystem_WhenGOIsNonUniformlyScaled_ConvexColliderHasBakedScale()
        {
            TestNonUniformScaleOnCollider(new[] {typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)}, gameObjectToConvert =>
            {
                // create a primitive cube which we will assign to the game object used in the test
                var cubeGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var cubeMeshFilter = cubeGameObject.GetComponent<MeshFilter>();
                var cubeMeshRenderer = cubeGameObject.GetComponent<MeshRenderer>();
                Assert.That(cubeMeshFilter != null && cubeMeshFilter.sharedMesh != null && cubeMeshRenderer != null);

                // set up the test game object with a mesh filter and renderer of a cube
                var meshFilter = gameObjectToConvert.GetComponent<MeshFilter>();
                meshFilter.mesh = cubeMeshFilter.sharedMesh;
                var meshRenderer = gameObjectToConvert.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = cubeMeshRenderer.sharedMaterial;

                // assign mesh to shape and make it a convex shape
                var shape = Child.GetComponent<PhysicsShapeAuthoring>();
                Assert.That(shape != null);
                shape.SetConvexHull(ConvexHullGenerationParameters.Default, cubeMeshFilter.sharedMesh);

                UnityEngine.Object.DestroyImmediate(cubeGameObject);
            });
        }

        /// <summary>
        /// Test that when a game object contains non-uniform scale in world space, a sphere collider has the non-uniform
        /// scale baked in.
        /// </summary>
        [Test]
        public void PhysicsShapeConversionSystem_WhenGOIsNonUniformlyScaled_SphereColliderHasBakedScale()
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                Array.Empty<Type>(),
                new[] {typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)}
            );

            // induce non-uniform scale in the child collider
            var nonUniformScale = new Vector3(1, 2, 3);
            Child.transform.localScale = nonUniformScale;

            var sphereShape = Child.GetComponent<PhysicsShapeAuthoring>();
            var unscaledRadius = 1.23f;
            sphereShape.SetSphere(new SphereGeometry { Radius = unscaledRadius });

            var expectedRadius = unscaledRadius * math.cmax(nonUniformScale);

            TestConvertedData<PhysicsCollider>((world, entities, colliders) =>
            {
                // expect there to be a LocalTransform component with identity scale
                var entity = entities[0];
                Assert.That(world.EntityManager.HasComponent<LocalTransform>(entity), Is.True);
                var localTransform = world.EntityManager.GetComponentData<LocalTransform>(entity);
                Assert.That(localTransform.Scale, Is.PrettyCloseTo(1));

                // expect there to be a PostTransformMatrix component
                Assert.That(world.EntityManager.HasComponent<PostTransformMatrix>(entity), Is.True);

                var postTransformMatrix = world.EntityManager.GetComponentData<PostTransformMatrix>(entity);
                // expect the post transform matrix to have non-uniform scale but no shear
                Assert.That(postTransformMatrix.Value.HasNonUniformScale());
                Assert.That(postTransformMatrix.Value.HasShear(), Is.False);

                // check if the sphere collider geometry is as expected
                unsafe
                {
                    var sphereColliderPtr = (SphereCollider*)colliders[0].ColliderPtr;
                    Assert.That(sphereColliderPtr->Radius, Is.PrettyCloseTo(expectedRadius));
                }

                TestScaleChange(world, entity);
            }, 1);
        }

        /// <summary>
        /// Test that when a game object contains non-uniform scale in world space, a capsule collider has the non-uniform
        /// scale baked in.
        /// </summary>
        [Test]
        public void PhysicsShapeConversionSystem_WhenGOIsNonUniformlyScaled_CapsuleColliderHasBakedScale()
        {
            CreateHierarchy(
                Array.Empty<Type>(),
                Array.Empty<Type>(),
                new[] {typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)}
            );

            // induce non-uniform scale in the child collider
            var nonUniformScale = new Vector3(2, 3, 4);
            Child.transform.localScale = nonUniformScale;

            var capsuleShape = Child.GetComponent<PhysicsShapeAuthoring>();
            var unscaledRadius = 1.23f;
            var unscaledHeight = 4.2f;

            // capsule with z-axis as central axis
            capsuleShape.SetCapsule(new CapsuleGeometryAuthoring() { Height = unscaledHeight, Radius = unscaledRadius, Orientation = quaternion.identity});
            int directionIndex = 2; // z-axis

            var expectedRadius = unscaledRadius * math.cmax(new float3(nonUniformScale) { [directionIndex] = 0f });
            var expectedHeight = unscaledHeight * nonUniformScale[directionIndex];

            TestConvertedData<PhysicsCollider>((world, entities, colliders) =>
            {
                // expect there to be a LocalTransform component with identity scale
                var entity = entities[0];
                Assert.That(world.EntityManager.HasComponent<LocalTransform>(entity), Is.True);
                var localTransform = world.EntityManager.GetComponentData<LocalTransform>(entity);
                Assert.That(localTransform.Scale, Is.PrettyCloseTo(1));

                // expect there to be a PostTransformMatrix component
                Assert.That(world.EntityManager.HasComponent<PostTransformMatrix>(entity), Is.True);

                var postTransformMatrix = world.EntityManager.GetComponentData<PostTransformMatrix>(entity);
                // expect the post transform matrix to have non-uniform scale but no shear
                Assert.That(postTransformMatrix.Value.HasNonUniformScale());
                Assert.That(postTransformMatrix.Value.HasShear(), Is.False);

                // check if the sphere collider geometry is as expected
                unsafe
                {
                    var capsuleColliderPtr = (CapsuleCollider*)colliders[0].ColliderPtr;
                    Assert.That(capsuleColliderPtr->Radius, Is.PrettyCloseTo(expectedRadius));

                    var height = math.distance(capsuleColliderPtr->Vertex0, capsuleColliderPtr->Vertex1) + 2 * capsuleColliderPtr->Radius;
                    Assert.That(height, Is.PrettyCloseTo(expectedHeight));
                }

                TestScaleChange(world, entity);
            }, 1);
        }

        private static Vector3[] GetLocalScalesUniform()
        {
            return new[]
            {
                new Vector3(1, 1, 1),
                new Vector3(0.8f, 0.8f, 0.8f)
            };
        }

        /// <summary>
        /// Tests that colliders in provided entities have expected bounds, assuming they are all baked from the provided shape type.
        /// </summary>
        void TestCollidersHaveExpectedBounds(ShapeType shapeType, World world, NativeArray<Entity> entities, NativeArray<PhysicsCollider> colliders,
            List<Tuple<Bounds, Transform>> expectedBounds)
        {
            // expect the colliders to have the same size as the mesh bounds
            var foundIndices = new NativeHashSet<int>(entities.Length, Allocator.Temp);
            var manager = world.EntityManager;
            for (int i = 0; i < colliders.Length; i++)
            {
                var entity = entities[i];
                GetRigidBodyTransformationData(ref manager, entity, out var colliderWorldTransform,
                    out var colliderScale, out var colliderLocalToWorld);

                var matrixPrettyCloseTo = new MatrixPrettyCloseConstraint(colliderLocalToWorld.Value);
                // find the mesh bounds that correspond to the collider by comparing the entity's transform
                // with the mesh bounds' transform
                var expectedBoundsIndex = expectedBounds.FindIndex(element =>
                    matrixPrettyCloseTo.ApplyTo((float4x4)element.Item2.localToWorldMatrix).IsSuccess);
                Assert.That(expectedBoundsIndex, Is.Not.EqualTo(-1));

                var notAlreadyPresent = foundIndices.Add(expectedBoundsIndex);
                Assert.That(notAlreadyPresent, NUnit.Framework.Is.True);

                var collider = colliders[i];
                var expectedBoundsElement = expectedBounds[expectedBoundsIndex];
                var colliderBounds = collider.Value.Value.CalculateAabb(colliderWorldTransform, colliderScale);
                var actualBoundsSize = colliderBounds.Extents;
                var expectedBoundsSize = expectedBoundsElement.Item1.size;
                var actualBoundsCenter = colliderBounds.Center;
                var expectedBoundsCenter = expectedBoundsElement.Item1.center;
                if (shapeType == ShapeType.Plane)
                {
                    // ignore default plane axis
                    actualBoundsSize[0] = expectedBoundsSize[0] = actualBoundsCenter[0] = expectedBoundsCenter[0] = 0;
                }

                Assert.That(actualBoundsSize, Is.PrettyCloseTo(expectedBoundsSize));
                Assert.That(actualBoundsCenter, Is.PrettyCloseTo(expectedBoundsCenter));
            }
        }

        /// <summary>
        /// Tests that there is only one compound collider and that its bounds correspond to the union of the provided bounds, assuming they are all baked from the provided shape type.
        /// </summary>
        protected void TestCompoundColliderHasExpectedUnionBounds(ShapeType shapeType, World world, NativeArray<Entity> entities, NativeArray<PhysicsCollider> colliders,
            List<Tuple<Bounds, Transform>> expectedBounds)
        {
            // expect only one compound collider in this case
            Assert.That(colliders.Length, Is.EqualTo(1));

            ref var compoundCollider = ref colliders[0].Value.Value;
            Assert.That(compoundCollider.Type, Is.EqualTo(ColliderType.Compound));

            // calculate union of the expected bounds for comparison
            var expectedUnionBounds = new Bounds();
            foreach (var expectedBound in expectedBounds)
            {
                expectedUnionBounds.Encapsulate(expectedBound.Item1);
            }

            // expect the compound collider to have the same size as the union of the mesh bounds
            var entity = entities[0];
            var manager = world.EntityManager;
            GetRigidBodyTransformationData(ref manager, entity, out var colliderWorldTransform,
                out var colliderScale, out var colliderLocalToWorld);

            var compoundColliderBounds = compoundCollider.CalculateAabb(colliderWorldTransform, colliderScale);
            var actualBoundsSize = compoundColliderBounds.Extents;
            var expectedBoundsSize = expectedUnionBounds.size;
            var actualBoundsCenter = compoundColliderBounds.Center;
            var expectedBoundsCenter = expectedUnionBounds.center;
            if (shapeType == ShapeType.Plane)
            {
                // ignore default plane axis
                actualBoundsSize[0] = expectedBoundsSize[0] = actualBoundsCenter[0] = expectedBoundsCenter[0] = 0;
            }

            Assert.That(actualBoundsSize, Is.PrettyCloseTo(expectedBoundsSize));
            Assert.That(actualBoundsCenter, Is.PrettyCloseTo(expectedBoundsCenter));
        }

        [Test]
        public void PhysicsShapeConversionSystem_NonStaticRigidbodyHierarchy_WithDifferentScales_CollidersHaveExpectedSize([Values(BodyMotionType.Kinematic, BodyMotionType.Dynamic)] BodyMotionType bodyMotionType, [Values] ShapeType shapeType, [Values] bool gameObjectIsStatic, [ValueSource(nameof(GetLocalScalesUniform))] Vector3 localScale)
        {
            TestCorrectColliderSizeInHierarchy(new[] {typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)},
                () =>
                {
                    Root.transform.localScale = Parent.transform.localScale = Child.transform.localScale = localScale;

                    foreach (var physicsShape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
                    {
                        SetDefaultShape(physicsShape, shapeType);
                    }

                    foreach (var rigidbody in Root.GetComponentsInChildren<PhysicsBodyAuthoring>())
                    {
                        rigidbody.MotionType = bodyMotionType;
                    }
                },
                3,
                (world, entities, colliders, expectedBounds) =>
                    TestCollidersHaveExpectedBounds(shapeType, world, entities, colliders, expectedBounds)
            );
        }

        [Test]
        public void PhysicsShapeConversionSystem_StaticRigidbodyHierarchy_WithIdentityScale_CollidersHaveExpectedSize([Values] ShapeType shapeType, [Values] bool gameObjectIsStatic)
        {
            TestCorrectColliderSizeInHierarchy(new[] {typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)},
                () =>
                {
                    Root.transform.localScale = Parent.transform.localScale = Child.transform.localScale = new Vector3(1, 1, 1);

                    foreach (var physicsShape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
                    {
                        SetDefaultShape(physicsShape, shapeType);
                    }

                    foreach (var rigidbody in Root.GetComponentsInChildren<PhysicsBodyAuthoring>())
                    {
                        rigidbody.MotionType = BodyMotionType.Static;
                    }
                },
                3,
                (world, entities, colliders, expectedBounds) =>
                    TestCollidersHaveExpectedBounds(shapeType, world, entities, colliders, expectedBounds)
            );
        }

        [Test]
        public void PhysicsShapeConversionSystem_RigidbodyHierarchy_WithNonUniformScale_ColliderHasExpectedSize([Values] BodyMotionType bodyMotionType, [Values(ShapeType.Box, ShapeType.Mesh, ShapeType.ConvexHull)] ShapeType shapeType, [Values] bool gameObjectIsStatic)
        {
            const bool expectCompound = false;
            TestCorrectColliderSizeInHierarchy(new[] {typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)},
                () =>
                {
                    Root.transform.localScale = new Vector3(0.75f, 0.5f, 1);
                    Parent.transform.localScale = new Vector3(1, 0.75f, 0.5f);
                    Child.transform.localScale = new Vector3(0.5f, 1, 0.75f);

                    foreach (var rigidbody in Root.GetComponentsInChildren<PhysicsBodyAuthoring>())
                    {
                        rigidbody.MotionType = bodyMotionType;
                    }

                    foreach (var physicsShape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
                    {
                        SetDefaultShape(physicsShape, shapeType);
                    }
                }, expectCompound
            );
        }

        [Test]
        public void PhysicsShapeConversionSystem_ColliderHierarchy_WithDifferentScales_CollidersHaveExpectedSize([Values] ShapeType shapeType, [Values] bool gameObjectIsStatic, [ValueSource(nameof(GetLocalScalesUniform))] Vector3 localScale)
        {
            TestCorrectColliderSizeInHierarchy(new[] {typeof(PhysicsShapeAuthoring)},
                () =>
                {
                    Root.transform.localScale = Parent.transform.localScale = Child.transform.localScale = localScale;

                    foreach (var physicsShape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
                    {
                        SetDefaultShape(physicsShape, shapeType);
                    }
                },
                1,
                (world, entities, colliders, expectedBounds) =>
                    TestCompoundColliderHasExpectedUnionBounds(shapeType, world, entities, colliders, expectedBounds)
            );
        }

        [Test]
        public void PhysicsShapeConversionSystem_ColliderHierarchy_WithNonUniformScale_ColliderHasExpectedSize([Values(ShapeType.Box, ShapeType.Mesh, ShapeType.ConvexHull)] ShapeType shapeType, [Values] bool gameObjectIsStatic)
        {
            const bool expectCompound = true;
            TestCorrectColliderSizeInHierarchy(new[] {typeof(PhysicsShapeAuthoring)},
                () =>
                {
                    Root.transform.localScale = new Vector3(0.75f, 0.5f, 1);
                    Parent.transform.localScale = new Vector3(1, 0.75f, 0.5f);
                    Child.transform.localScale = new Vector3(0.5f, 1, 0.75f);

                    foreach (var physicsShape in Root.GetComponentsInChildren<PhysicsShapeAuthoring>())
                    {
                        SetDefaultShape(physicsShape, shapeType);
                    }
                }, expectCompound
            );
        }
    }
}
