using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Unity.Physics.Tests.Authoring
{
    class PhysicsShapeExtensions_Baking_IntegrationTests
    {
        PhysicsShapeAuthoring m_Shape;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Shape = new GameObject(GetType().Name, typeof(PhysicsShapeAuthoring)).GetComponent<PhysicsShapeAuthoring>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (m_Shape != null)
                GameObject.DestroyImmediate(m_Shape.gameObject);
        }

        [SetUp]
        public void SetUp()
        {
            m_Shape.transform.localScale = Vector3.one;
        }

        static EulerAngles GetOrientation(float x, float y, float z) =>
            new EulerAngles { Value = new float3(x, y, z), RotationOrder = math.RotationOrder.ZXY };

        const float k_OrientationTolerance = 0.001f;

        // following are slow tests used for local regression testing only
        /*
        [Test]
        public void GetBakedBoxProperties_WhenNoShear_ReturnsInputOrientation(
            [Values(1f, 2f, 3f)] float sx, [Values(1f, 2f, 3f)] float sy, [Values(1f, 2f, 3f)] float sz,
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            m_Shape.transform.localScale = new Vector3(sx, sy, sz);
            var expectedOrientation = GetOrientation(rx, ry, rz);
            m_Shape.SetBox(new float3(0f), new float3(1f), expectedOrientation);

            m_Shape.GetBakedBoxProperties(
                out var center, out var size, out var actualOrientation, out var convexRadius
            );

            Assert.That(
                (quaternion)actualOrientation,
                Is.OrientedEquivalentTo(expectedOrientation).EachAxisWithin(k_OrientationTolerance)
            );
        }

        [Test]
        public void GetBakedBoxProperties_WhenScaleIsIdentity_ReturnsInputSize(
            [Values(1f, 2f, 3f)] float sx, [Values(1f, 2f, 3f)] float sy, [Values(1f, 2f, 3f)] float sz,
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            var expectedSize = new float3(sx, sy, sz);
            m_Shape.SetBox(new float3(0f), expectedSize, GetOrientation(rx, ry, rz));

            m_Shape.GetBakedBoxProperties(
                out var center, out var actualSize, out var orientation, out var convexRadius
            );

            Assert.That(actualSize, Is.EqualTo(expectedSize));
        }

        [Test]
        public void GetBakedBoxProperties_WhenScaleIsIdentity_ReturnsInputBevelRadius(
            [Values(1f, 2f, 3f)] float sx, [Values(1f, 2f, 3f)] float sy, [Values(1f, 2f, 3f)] float sz,
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            m_Shape.SetBox(new float3(0f), new float3(sx, sy, sz), GetOrientation(rx, ry, rz));
            var expectedBevelRadius = ConvexHullGenerationParameters.Default.BevelRadius * 0.5f;
            m_Shape.BevelRadius = expectedBevelRadius;

            m_Shape.GetBakedBoxProperties(
                out var center, out var size, out var orientation, out var actualBevelRadius
            );

            Assert.That(actualBevelRadius, Is.EqualTo(expectedBevelRadius));
        }

        [Test]
        public void GetBakedCapsuleProperties_WhenNoShear_ReturnsInputOrientation(
            [Values(1f, 2f, 3f)] float sx, [Values(1f, 2f, 3f)] float sy, [Values(1f, 2f, 3f)] float sz,
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            m_Shape.transform.localScale = new Vector3(sx, sy, sz);
            var expectedOrientation = GetOrientation(rx, ry, rz);
            m_Shape.SetCapsule(0f, 2f, 0.5f, expectedOrientation);

            m_Shape.GetBakedCapsuleProperties(
                out var center, out var height, out var radius, out var actualOrientation, out var v0, out var v1
            );

            Assert.That(
                (quaternion)actualOrientation,
                Is.OrientedEquivalentTo(expectedOrientation).EachAxisWithin(k_OrientationTolerance)
            );
        }

        [Test]
        public void GetBakedCapsuleProperties_WhenScaleIsIdentity_ReturnsInputHeight(
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            var expectedHeight = math.PI;
            m_Shape.SetCapsule(0f, expectedHeight, 1f, GetOrientation(rx, ry, rz));

            m_Shape.GetBakedCapsuleProperties(
                out var center, out var actualHeight, out var radius, out var orientation, out var v0, out var v1
            );

            Assert.That(actualHeight, Is.EqualTo(expectedHeight));
        }

        [Test]
        public void GetBakedCapsuleProperties_WhenScaleIsIdentity_ReturnsInputRadius(
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            var expectedRadius = math.PI;
            m_Shape.SetCapsule(0f, 10f, expectedRadius, GetOrientation(rx, ry, rz));

            m_Shape.GetBakedCapsuleProperties(
                out var center, out var height, out var actualRadius, out var orientation, out var v0, out var v1
            );

            Assert.That(actualRadius, Is.EqualTo(expectedRadius));
        }

        [Test]
        public void GetBakedCylinderProperties_WhenNoShear_ReturnsInputOrientation(
            [Values(1f, 2f, 3f)] float sx, [Values(1f, 2f, 3f)] float sy, [Values(1f, 2f, 3f)] float sz,
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            m_Shape.transform.localScale = new Vector3(sx, sy, sz);
            var expectedOrientation = GetOrientation(rx, ry, rz);
            m_Shape.SetCylinder(0f, 2f, 0.5f, expectedOrientation);

            m_Shape.GetBakedCylinderProperties(
                out var center, out var height, out var radius, out var actualOrientation, out var convexRadius
            );

            Assert.That((quaternion)actualOrientation, Is.OrientedEquivalentTo(expectedOrientation));
        }

        [Test]
        public void GetBakedCylinderProperties_WhenScaleIsIdentity_ReturnsInputHeight(
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            var expectedHeight = math.PI;
            m_Shape.SetCylinder(0f, expectedHeight, 1f, GetOrientation(rx, ry, rz));

            m_Shape.GetBakedCylinderProperties(
                out var center, out var actualHeight, out var radius, out var orientation, out var convexRadius
            );

            Assert.That(actualHeight, Is.EqualTo(expectedHeight));
        }

        [Test]
        public void GetBakedCylinderProperties_WhenScaleIsIdentity_ReturnsInputRadius(
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            var expectedRadius = math.PI;
            m_Shape.SetCylinder(0f, 10f, expectedRadius, GetOrientation(rx, ry, rz));

            m_Shape.GetBakedCylinderProperties(
                out var center, out var height, out var actualRadius, out var orientation, out var convexRadius
            );

            Assert.That(actualRadius, Is.EqualTo(expectedRadius));
        }

        [Test]
        public void GetBakedCylinderProperties_WhenScaleIsIdentity_ReturnsInputBevelRadius(
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            m_Shape.SetCylinder(0f, 2f, 0.5f, GetOrientation(rx, ry, rz));
            var expectedBevelRadius = ConvexHullGenerationParameters.Default.BevelRadius * 0.5f;
            m_Shape.BevelRadius = expectedBevelRadius;

            m_Shape.GetBakedCylinderProperties(
                out var center, out var height, out var radius, out var orientation, out var actualBevelRadius
            );

            Assert.That(actualBevelRadius, Is.EqualTo(expectedBevelRadius));
        }

        [Test]
        public void GetBakedSphereProperties_WhenNoShear_ReturnsInputOrientation(
            [Values(1f, 2f, 3f)] float sx, [Values(1f, 2f, 3f)] float sy, [Values(1f, 2f, 3f)] float sz,
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            m_Shape.transform.localScale = new Vector3(sx, sy, sz);
            var orientation = GetOrientation(rx, ry, rz);
            var expectedOrientation = (quaternion)orientation;
            m_Shape.SetSphere(new float3(0f), 1f, orientation);

            m_Shape.GetBakedSphereProperties(out var center, out var radius, out var actualOrientation);

            Assert.That(
                (quaternion)actualOrientation,
                Is.OrientedEquivalentTo(expectedOrientation).EachAxisWithin(k_OrientationTolerance)
            );
        }

        [Test]
        public void GetBakedSphereProperties_WhenScaleIsIdentity_ReturnsInputRadius(
            [Values(-90f, 0f, 90f)] float rx, [Values(-90f, 0f, 90f)] float ry, [Values(-90f, 0f, 90f)] float rz
        )
        {
            var orientation = GetOrientation(rx, ry, rz);
            var expectedRadius = 1f;
            m_Shape.SetSphere(new float3(0f), expectedRadius, orientation);

            m_Shape.GetBakedSphereProperties(out var center, out var actualRadius, out orientation);

            Assert.That(actualRadius, Is.EqualTo(expectedRadius));
        }

        [Test]
        public void GetBakedConvexProperties_WhenScaleIsIdentity_ReturnsInputBevelRadius()
        {
            var expectedBevelRadius = ConvexHullGenerationParameters.Default.BevelRadius * 0.5f;
            m_Shape.SetConvexHull(Resources.GetBuiltinResource<UnityEngine.Mesh>("New-Sphere.fbx"));
            m_Shape.BevelRadius = expectedBevelRadius;

            ConvexHullGenerationParameters actual;
            using (var pointCloud = new NativeList<float3>(Allocator.TempJob))
                m_Shape.GetBakedConvexProperties(pointCloud, out actual);

            Assert.That(actual.BevelRadius, Is.EqualTo(expectedBevelRadius));
        }
        */
    }
}
