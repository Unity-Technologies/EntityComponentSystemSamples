#if UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections.Tests;

namespace Unity.Entities.Tests
{
    [TestFixture]
    public class EntitiesBurstCompatibilityTests : BurstCompatibilityTests
    {
        public EntitiesBurstCompatibilityTests()
            : base(new [] { "Unity.Entities", "Unity.Entities.BurstCompatibilityTestCodeGen" },
                "Assets/Tests/Editmode/BurstCompatibility/Unity.Entities.BurstCompatibilityTestCodeGen/_generated_burst_tests.cs",
                "Unity.Entities.BurstCompatibilityTestCodeGen")
        {
        }
    }
}
#endif
