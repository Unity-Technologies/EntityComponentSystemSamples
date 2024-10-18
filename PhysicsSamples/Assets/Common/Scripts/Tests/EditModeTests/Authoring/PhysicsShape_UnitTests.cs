using NUnit.Framework;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Unity.Physics.Tests.Authoring
{
    class PhysicsShape_UnitTests
    {
        const float k_Tolerance = 0.001f;

        PhysicsShapeAuthoring m_Shape;

        [SetUp]
        public void SetUp() => m_Shape = new GameObject("Shape").AddComponent<PhysicsShapeAuthoring>();

        [TearDown]
        public void TearDown()
        {
            if (m_Shape != null)
                GameObject.DestroyImmediate(m_Shape.gameObject);
        }

        [Test]
        public void SetBoxProperties_WithSizeLessThanZero_ClampsToZero()
        {
            m_Shape.SetBox(new BoxGeometry { Size = -3f, Orientation = quaternion.identity });

            var box = m_Shape.GetBoxProperties();

            Assert.That(box.Size, Is.EqualTo(new float3(0f)));
        }

        [Test]
        public void GetCapsuleProperties_WhenShapeIsBox_HeightIsMaxDimension(
            [Values(0f, 1f, 2f, 3f)] float sizeX,
            [Values(0f, 1f, 2f, 3f)] float sizeY,
            [Values(0f, 1f, 2f, 3f)] float sizeZ
        )
        {
            var size = new float3(sizeX, sizeY, sizeZ);
            m_Shape.SetBox(new BoxGeometry { Size = size, Orientation = quaternion.identity });

            var capsule = m_Shape.GetCapsuleProperties();

            var height = capsule.Height;
            Assert.That(height, Is.EqualTo(math.cmax(size)));
        }

        [Test]
        public void GetCapsuleProperties_WhenShapeIsBox_RadiusIsHalfSecondMaxDimension(
            [Values(0f, 1f, 2f, 3f)] float sizeX,
            [Values(0f, 1f, 2f, 3f)] float sizeY,
            [Values(0f, 1f, 2f, 3f)] float sizeZ
        )
        {
            var size = new float3(sizeX, sizeY, sizeZ);
            m_Shape.SetBox(new BoxGeometry { Size = size, Orientation = quaternion.identity });

            var capsule = m_Shape.GetCapsuleProperties();

            var cmaxI = size.GetMaxAxis();
            var expectedRadius = 0.5f * math.cmax(cmaxI == 0 ? size.yz : cmaxI == 1 ? size.xz : size.xy);
            Assert.That(capsule.Radius, Is.EqualTo(expectedRadius));
        }

        static readonly TestCaseData[] k_CapsuleOrientationTestCases =
        {
            new TestCaseData(new float3(2f, 1f, 1f), new float3(1f, 0f, 0f)).SetName("Aligned to x-axis"),
            new TestCaseData(new float3(1f, 2f, 1f), new float3(0f, 1f, 0f)).SetName("Aligned to y-axis"),
            new TestCaseData(new float3(1f, 1f, 2f), new float3(0f, 0f, 1f)).SetName("Aligned to z-axis")
        };
        [TestCaseSource(nameof(k_CapsuleOrientationTestCases))]
        public void GetCapsuleProperties_WhenShapeIsElongatedBox_OrientationPointsDownLongAxis(float3 boxSize, float3 expectedLookVector)
        {
            m_Shape.SetBox(new BoxGeometry { Size = boxSize, Orientation = quaternion.identity });

            var capsule = m_Shape.GetCapsuleProperties();

            var lookVector = math.mul(capsule.Orientation, new float3(0f, 0f, 1f));
            Assert.That(
                math.dot(lookVector, expectedLookVector), Is.EqualTo(1f).Within(k_Tolerance),
                $"Expected {expectedLookVector} but got {lookVector}"
            );
        }

        [Test]
        public void GetCapsuleProperties_WhenShapeIsRotatedElongatedBox_MidpointIsBoxCenter(
            [Values(0f, 1f, 2f, 3f)] float sizeX,
            [Values(0f, 1f, 2f, 3f)] float sizeY,
            [Values(0f, 1f, 2f, 3f)] float sizeZ
        )
        {
            var size = new float3(sizeX, sizeY, sizeZ);
            var orientation = quaternion.LookRotation(new float3(1f), math.up());
            var expectedCenter = new float3(4f, 5f, 6f);
            m_Shape.SetBox(new BoxGeometry { Size = size, Center = expectedCenter, Orientation = orientation });

            var capsule = m_Shape.GetCapsuleProperties();

            Assert.That(capsule.Center, Is.EqualTo(expectedCenter));
        }

        [Test]
        public void GetCylinderProperties_WhenShapeIsBox_HeightIsDeviantDimension(
            [Values(0f, 1f, 2f, 3f)] float sizeX,
            [Values(0f, 1f, 2f, 3f)] float sizeY,
            [Values(0f, 1f, 2f, 3f)] float sizeZ
        )
        {
            var size = new float3(sizeX, sizeY, sizeZ);
            m_Shape.SetBox(new BoxGeometry { Size = size, Orientation = quaternion.identity });

            var cylinder = m_Shape.GetCylinderProperties();

            var heightAxis = size.GetDeviantAxis();
            Assert.That(cylinder.Height, Is.EqualTo(size[heightAxis]));
        }

        [Test]
        public void GetCylinderProperties_WhenShapeIsBox_RadiusIsHalfMaxHomogenousDimension(
            [Values(0f, 1f, 2f, 3f)] float sizeX,
            [Values(0f, 1f, 2f, 3f)] float sizeY,
            [Values(0f, 1f, 2f, 3f)] float sizeZ
        )
        {
            var size = new float3(sizeX, sizeY, sizeZ);
            m_Shape.SetBox(new BoxGeometry { Size = size, Orientation = quaternion.identity });

            var cylinder = m_Shape.GetCylinderProperties();

            var heightAxis = size.GetDeviantAxis();
            var expectedRadius = 0.5f * math.cmax(heightAxis == 0 ? size.yz : heightAxis == 1 ? size.xz : size.xy);
            Assert.That(cylinder.Radius, Is.EqualTo(expectedRadius));
        }

        [Test]
        public void GetSphereProperties_WhenShapeIsBox_RadiusIsHalfMaxDimension(
            [Values(0f, 1f, 2f, 3f)] float sizeX,
            [Values(0f, 1f, 2f, 3f)] float sizeY,
            [Values(0f, 1f, 2f, 3f)] float sizeZ
        )
        {
            var size = new float3(sizeX, sizeY, sizeZ);
            m_Shape.SetBox(new BoxGeometry { Size = size, Orientation = quaternion.identity });

            var sphere = m_Shape.GetSphereProperties(out quaternion _);

            var expectedRadius = 0.5f * math.cmax(size);
            Assert.That(sphere.Radius, Is.EqualTo(expectedRadius));
        }

        static readonly TestCaseData[] k_PlaneSizeTestCases =
        {
            new TestCaseData(new float3(2f, 3f, 1f), 0, 1).SetName("xy"),
            new TestCaseData(new float3(2f, 1f, 3f), 0, 2).SetName("xz"),
            new TestCaseData(new float3(1f, 2f, 3f), 1, 2).SetName("yz")
        };

        [TestCaseSource(nameof(k_PlaneSizeTestCases))]
        public void GetPlaneProperties_WhenShapeIsBox_SizeIsTwoGreatestDimensions(float3 boxSize, int ax1, int ax2)
        {
            m_Shape.SetBox(new BoxGeometry { Size = boxSize, Orientation = quaternion.identity });

            m_Shape.GetPlaneProperties(out _, out var size, out quaternion _);

            Assert.That(
                new[] { size.x, size.y }, Is.EquivalentTo(new[] { boxSize[ax1], boxSize[ax2] }),
                "Plane dimensions did not match two greatest dimensions of original box"
            );
        }

        static readonly TestCaseData[] k_PlaneOrientationTestCases =
        {
            new TestCaseData(new float3(3f, 2f, 1f), quaternion.LookRotation(new float3(1f, 0f, 0f), new float3(0f, 0f, 1f))).SetName("look x, up z"),
            new TestCaseData(new float3(2f, 3f, 1f), quaternion.LookRotation(new float3(0f, 1f, 0f), new float3(0f, 0f, 1f))).SetName("look y, up z"),
            new TestCaseData(new float3(3f, 1f, 2f), quaternion.LookRotation(new float3(1f, 0f, 0f), new float3(0f, 1f, 0f))).SetName("look x, up y"),
            new TestCaseData(new float3(2f, 1f, 3f), quaternion.LookRotation(new float3(0f, 0f, 1f), new float3(0f, 1f, 0f))).SetName("look z, up y"),
            new TestCaseData(new float3(1f, 3f, 2f), quaternion.LookRotation(new float3(0f, 1f, 0f), new float3(1f, 0f, 0f))).SetName("look y, up x"),
            new TestCaseData(new float3(1f, 2f, 3f), quaternion.LookRotation(new float3(0f, 0f, 1f), new float3(1f, 0f, 0f))).SetName("look z, up x")
        };

        [TestCaseSource(nameof(k_PlaneOrientationTestCases))]
        public void GetPlaneProperties_WhenShapeIsBox_OrientationPointsDownLongAxisUpFlatAxis(float3 boxSize, quaternion expected)
        {
            m_Shape.SetBox(new BoxGeometry { Size = boxSize, Orientation = quaternion.identity });

            m_Shape.GetPlaneProperties(out _, out _, out quaternion orientation);

            var expectedLook = math.mul(expected, new float3 { z = 1f });
            var expectedUp = math.mul(expected, new float3 { y = 1f });
            var actualLook = math.mul(orientation, new float3 { z = 1f });
            var actualUp = math.mul(orientation, new float3 { y = 1f });
            var dotProducts = math.abs(new float3(
                math.dot(expectedLook, actualLook),
                math.dot(expectedUp, actualUp),
                0f
            ));
            Assert.That(
                dotProducts, Is.PrettyCloseTo(new float3(1f, 1f, 0f)),
                $"Expected look axis to be parallel to {expectedLook} and up axis to be parallel to {expectedUp} but got {actualLook} and {actualUp}"
            );
        }

        static readonly TestCaseData[] k_DeviantAxisTestCases =
        {
            new TestCaseData(new float3(0f,  1f, 1f)).Returns(0).SetName("Smallest axis, other axes identical"),
            new TestCaseData(new float3(1f,  2f, 1f)).Returns(1).SetName("Largest axis, other axes identical"),
            new TestCaseData(new float3(5f,  8f, 1f)).Returns(2).SetName("Smallest axis, other axes differ"),
            new TestCaseData(new float3(9f,  2f, 3f)).Returns(0).SetName("Largest axis, other axes differ"),
            new TestCaseData(new float3(-1f, -1f, 1f)).Returns(2).SetName("Only positive axis, other axes identical"),
            new TestCaseData(new float3(1f, -2f, 1f)).Returns(1).SetName("Only negative axis, other axes identical")
        };

        [TestCaseSource(nameof(k_DeviantAxisTestCases))]
        public int GetDeviantAxis_ReturnsTheMostDifferentAxis(float3 v)
        {
            return v.GetDeviantAxis();
        }

        static readonly TestCaseData[] k_MaxAxisTestCases =
        {
            new TestCaseData(new float3(3f,  2f,  1f)).Returns(0).SetName("X-axis (all positive)"),
            new TestCaseData(new float3(3f,  2f, -4f)).Returns(0).SetName("X-axis (one negative with greater magnitude)"),
            new TestCaseData(new float3(2f,  3f,  1f)).Returns(1).SetName("Y-axis (all positive)"),
            new TestCaseData(new float3(-4f,  3f,  2f)).Returns(1).SetName("Y-axis (one negative with greater magnitude)"),
            new TestCaseData(new float3(1f,  2f,  3f)).Returns(2).SetName("Z-axis (all positive)"),
            new TestCaseData(new float3(2f, -4f,  3f)).Returns(2).SetName("Z-axis (one negative with greater magnitude)"),
        };

        [TestCaseSource(nameof(k_MaxAxisTestCases))]
        public int GetMaxAxis_ReturnsLargestAxis(float3 v)
        {
            return v.GetMaxAxis();
        }
    }
}
