using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Physics.Editor;
using UnityEditor;

namespace Unity.Physics.Tests.Editor
{
    class StatusMessageUtility_UnitTests
    {
        static IEnumerable k_GetMatrixStatusMessageTestCases = new[]
        {
            new TestCaseData(new[] { MatrixState.UniformScale, MatrixState.ZeroScale }, "zero").Returns(MessageType.Warning).SetName("At least one zero"),
            new TestCaseData(new[] { MatrixState.UniformScale, MatrixState.NonUniformScale }, "(non-uniform|performance)").Returns(MessageType.Warning).SetName("At least one non-uniform"),
            new TestCaseData(new[] { MatrixState.UniformScale, MatrixState.NotValidTRS }, "(not |in)valid").Returns(MessageType.Error).SetName("At least one invalid"),
            new TestCaseData(new[] { MatrixState.UniformScale, MatrixState.UniformScale }, "^$").Returns(MessageType.None).SetName("All uniform")
        };

        [TestCaseSource(nameof(k_GetMatrixStatusMessageTestCases))]
        public MessageType GetMatrixStatusMessage_WithStateCombination_MessageContainsExpectedKeywords(
            IReadOnlyList<MatrixState> matrixStates, string keywords
        )
        {
            var result = StatusMessageUtility.GetMatrixStatusMessage(matrixStates, out var message);

            Assert.That(message, Does.Match(keywords));

            return result;
        }
    }
}
