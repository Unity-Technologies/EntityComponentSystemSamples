using NUnit.Framework;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Unity.Physics.Tests.Authoring
{
    class CapsuleGeometryAuthoring_UnitTests
    {
        [Test]
        public void SetOrientation_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new CapsuleGeometryAuthoring { Orientation = quaternion.identity });
        }
    }
}
