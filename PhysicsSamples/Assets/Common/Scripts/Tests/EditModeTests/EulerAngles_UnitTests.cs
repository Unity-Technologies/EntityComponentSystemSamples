using NUnit.Framework;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Unity.Physics.Tests.Authoring
{
    class EulerAngles_UnitTests
    {
        [BurstCompile(CompileSynchronously = true)]
        struct SetValueFromBurstJob : IJob
        {
            public void Execute() => new EulerAngles().SetValue(quaternion.identity);
        }

        [Test]
        public void SetValue_WhenCalledFromBurstJob_DoesNotThrow() => new SetValueFromBurstJob().Run();

        static readonly quaternion k_NotIdentityQuaternion = math.mul(
            math.mul(
                quaternion.AxisAngle(new float3 { z = 1f }, math.radians(45f)),
                quaternion.AxisAngle(new float3 { y = 1f }, math.radians(45f))
                ),  quaternion.AxisAngle(new float3 { x = 1f }, math.radians(45f))
        );

        static readonly TestCaseData[] k_TestCases =
        {
            new TestCaseData(math.RotationOrder.XYZ, quaternion.identity, float3.zero).SetName("XYZ (identity)"),
            new TestCaseData(math.RotationOrder.YZX, quaternion.identity, float3.zero).SetName("YZX (identity)"),
            new TestCaseData(math.RotationOrder.ZXY, quaternion.identity, float3.zero).SetName("ZXY (identity)"),
            new TestCaseData(math.RotationOrder.XZY, quaternion.identity, float3.zero).SetName("XZY (identity)"),
            new TestCaseData(math.RotationOrder.YXZ, quaternion.identity, float3.zero).SetName("YXZ (identity)"),
            new TestCaseData(math.RotationOrder.ZYX, quaternion.identity, float3.zero).SetName("ZYX (identity)"),
            new TestCaseData(math.RotationOrder.XYZ, k_NotIdentityQuaternion, new float3(45f, 45f, 45f)).SetName("XYZ (not identity)"),
            new TestCaseData(math.RotationOrder.YZX, k_NotIdentityQuaternion, new float3(30.36119f, 59.63881f, 8.421058f)).SetName("YZX (not identity)"),
            new TestCaseData(math.RotationOrder.ZXY, k_NotIdentityQuaternion, new float3(8.421058f, 59.63881f, 30.36119f)).SetName("ZXY (not identity)"),
            new TestCaseData(math.RotationOrder.XZY, k_NotIdentityQuaternion, new float3(9.735609f, 54.73561f, 30f)).SetName("XZY (not identity)"),
            new TestCaseData(math.RotationOrder.YXZ, k_NotIdentityQuaternion, new float3(30f, 54.73561f, 9.735609f)).SetName("YXZ (not identity)"),
            new TestCaseData(math.RotationOrder.ZYX, k_NotIdentityQuaternion, new float3(16.32495f, 58.60028f, 16.32495f)).SetName("ZYX (not identity)")
        };

        [TestCaseSource(nameof(k_TestCases))]
        public void SetValue_WhenRotationOrder_ReturnsExpectedValue(
            math.RotationOrder rotationOrder, quaternion value, float3 expectedEulerAngles
        )
        {
            var eulerAngles = new EulerAngles { RotationOrder = rotationOrder };

            eulerAngles.SetValue(value);

            Assert.That(eulerAngles.Value, Is.PrettyCloseTo(expectedEulerAngles));
        }

        [Test]
        public void EulerToQuaternion_QuaternionToEuler_ResultingOrientationIsCloseToOriginal(
            [Values] math.RotationOrder rotationOrder,
            [Values(-90f, -45, 0f, 45, 90f)] float x,
            [Values(-90f, -45, 0f, 45, 90f)] float y,
            [Values(-90f, -45, 0f, 45, 90f)] float z
        )
        {
            var inputEulerAngles = new EulerAngles { RotationOrder = rotationOrder, Value = new float3(x, y, z) };
            var inputQuaternion = (quaternion)inputEulerAngles;
            Assume.That(math.abs(math.length(inputQuaternion.value)), Is.EqualTo(1.0f).Within(1e-05));

            EulerAngles outputEulerAngles = new EulerAngles { RotationOrder = inputEulerAngles.RotationOrder };
            outputEulerAngles.SetValue(inputQuaternion);
            quaternion outputQuaternion = (quaternion)outputEulerAngles;
            Assume.That(math.abs(math.length(outputQuaternion.value)), Is.EqualTo(1.0f).Within(1e-05));

            Assert.That(outputQuaternion, Is.OrientedEquivalentTo(inputQuaternion));
        }
    }
}
