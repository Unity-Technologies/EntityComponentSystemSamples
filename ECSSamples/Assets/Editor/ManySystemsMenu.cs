using System.IO;
using UnityEditor;

static class ManySystemsMenu
{
    [MenuItem("DOTS/StressTests ManySystems/Generate Systems C# Files")]
    private static void DOTS_StressTestsManySystems_GenerateSystemsCSharpFiles()
    {
        const int countSystems = 1000;
        var linearSystemsPath = "Assets/StressTests/ManySystems_Linear/ManySystems_Linear_Bootstrap_Systems.cs";
        using (var stream = new StreamWriter(linearSystemsPath))
        {
            stream.WriteLine("using Unity.Entities;");
            stream.WriteLine("namespace StressTests.ManySystems");
            stream.WriteLine("{");
            var systems = new string[] { "TestSystem_Schedule", "TestSystem_Run", "TestSystem_ScheduleReader", "TestSystem_RunReader" };
            for (var i = 0; i < countSystems; ++i)
            {
                foreach (var s in systems)
                {
                    stream.WriteLine($"    [UpdateInGroup(typeof(SimulationSystemGroup)), DisableAutoCreation, AlwaysUpdateSystem] class {s}_{i:0000} : {s} {{ }}");
                }
            }
            stream.WriteLine("}");
        }

        var complexSystemsPath = "Assets/StressTests/ManySystems_Complex/ManySystems_Complex_Bootstrap_Systems.cs";
        using (var stream = new StreamWriter(complexSystemsPath))
        {
            stream.WriteLine("using Unity.Entities;");
            stream.WriteLine("namespace StressTests.ManySystems");
            stream.WriteLine("{");
            var systems = new string[] { "TestSystem_Complex" };
            for (var i = 0; i < countSystems; ++i)
            {
                foreach (var s in systems)
                {
                    stream.WriteLine($"    [UpdateInGroup(typeof(SimulationSystemGroup)), DisableAutoCreation, AlwaysUpdateSystem] class {s}_{i:0000} : {s} {{ }}");
                }
            }
            stream.WriteLine("}");
        }
    }
}
