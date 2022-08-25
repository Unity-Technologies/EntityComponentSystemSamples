#if UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections.Tests;

[TestFixture]
public class CollectionsBurstTests : BurstCompatibilityTests
{
    public CollectionsBurstTests()
        : base(new [] { "Unity.Collections", "Unity.Collections.BurstCompatibilityTestCodeGen" },
            "Assets/Tests/Editmode/BurstCompatibility/Unity.Collections.BurstCompatibilityTestCodeGen/_generated_burst_tests.cs",
            "Unity.Collections.BurstCompatibilityTestCodeGen")
    {
    }
}
#endif
