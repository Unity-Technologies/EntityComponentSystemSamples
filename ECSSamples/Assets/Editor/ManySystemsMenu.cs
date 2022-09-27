using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Mathematics;
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
                    stream.WriteLine($"    [UpdateInGroup(typeof(SimulationSystemGroup)), DisableAutoCreation] class {s}_{i:0000} : {s} {{ }}");
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
            var systems = new string[] {"TestSystem_Complex"};
            for (var i = 0; i < countSystems; ++i)
            {
                foreach (var s in systems)
                {
                    stream.WriteLine($"    [UpdateInGroup(typeof(SimulationSystemGroup)), DisableAutoCreation] class {s}_{i:0000} : {s} {{ }}");
                }
            }

            stream.WriteLine("}");
        }
    }

    static string RandomEntity
    {
        get
        {
            return state.r.NextInt(10) switch
            {
                0 => $"{GetSingletonEntity(RandomComponentType)}",
                1 => $"{GetComponent("Parent", RandomEntity)}.Value",
                _ => "state.EntityManager.CreateEntity()"
            };
        }
    }

    static string RandomComponentType
    {
        get
        {
            return state.r.NextInt(4) switch
            {
                0 => "Rotation",
                1 => "LocalToWorld",
                2 => "LocalToParent",
                3 => "Parent",
                _ => "Translation"
            };
        }
    }

    static string RandomStatement
    {
        get
        {
            return state.r.NextInt(7) switch
            {
                0 => $"var statement{state.idx++} = {GetComponent(RandomComponentType, RandomEntity)};",
                1 => $"{SetComponent(RandomComponentType, RandomEntity)};",
                2 => $"var statement{state.idx++} = {HasComponent(RandomComponentType, RandomEntity)};",
                3 => $"var statement{state.idx++} = {GetComponentLookup(RandomComponentType,state.r.NextBool())};",
                4 => $"var statement{state.idx++} = {GetSingletonEntity(RandomComponentType)};",
                5 => $"{{\n                {RandomStatement}\n            }}",
                6 => $"{RandomStatement}\n            {RandomStatement}",
                _ => ";"
            };
        }
    }

    static string GetComponentLookup(string ComponentType, bool ReadOnly)
    {
        var readOnly = ReadOnly.ToString().ToLower();
        if (state.generate)
        {
            var fieldName = $"{ComponentType}_{readOnly}";
            state.fields.Add($"ComponentLookup<{ComponentType}> {fieldName};");
            state.createAssignments.Add($"{fieldName} = state.GetComponentLookup<{ComponentType}>({readOnly});");
            return $"{fieldName}";
        }

        return $"GetComponentLookup<{ComponentType}>({readOnly})";
    }

    static string GetComponent(string ComponentType, string Entity)
    {
        if (state.generate)
        {
            var fieldName = $"{ComponentType}_true";
            state.fields.Add($"ComponentLookup<{ComponentType}> {fieldName};");
            state.createAssignments.Add($"{fieldName} = state.GetComponentLookup<{ComponentType}>(true);");
            return $"{fieldName}[{Entity}]";
        }

        return $"GetComponent<{ComponentType}>({Entity})";
    }

    static string SetComponent(string ComponentType, string Entity)
    {
        if (state.generate)
        {
            var fieldName = $"{ComponentType}_false";
            state.fields.Add($"ComponentLookup<{ComponentType}> {fieldName};");
            state.createAssignments.Add($"{fieldName} = state.GetComponentLookup<{ComponentType}>(false);");
            return $"{fieldName}[{Entity}] = new {ComponentType}()";
        }

        return $"SetComponent({Entity}, new {ComponentType}())";
    }

    static string HasComponent(string ComponentType, string Entity)
    {
        if (state.generate)
        {
            var fieldName = $"{ComponentType}_false";
            state.fields.Add($"ComponentLookup<{ComponentType}> {fieldName};");
            state.createAssignments.Add($"{fieldName} = state.GetComponentLookup<{ComponentType}>(true);");
            return $"{fieldName}.HasComponent({Entity})";
        }

        return $"HasComponent<{ComponentType}>({Entity})";
    }

    static string GetSingletonEntity(string ComponentType)
    {
        if (state.generate)
        {
            var fieldName = $"{ComponentType}_query";
            state.fields.Add($"EntityQuery {fieldName};");
            state.createAssignments.Add($"{fieldName} = state.GetEntityQuery(typeof({ComponentType}));");
            return $"{fieldName}.GetSingletonEntity()";
        }

        return $"GetSingletonEntity<{ComponentType}>()";
    }

    static string OnUpdate
    {
        get
        {
            var builder = new StringBuilder();
            builder.AppendLine($"        public void OnUpdate (ref SystemState state)");
            builder.AppendLine($"        {{");
            for (var j = 0; j < 25; j++)
                builder.AppendLine($"            {RandomStatement}");
            builder.AppendLine($"        }}");

            return builder.ToString();
        }
    }

    static string OnCreate
    {
        get
        {
            var builder = new StringBuilder();
            foreach (var field in state.fields)
                builder.AppendLine($"        {field}");
            builder.AppendLine($"        public void OnCreate(ref SystemState state)");
            builder.AppendLine($"        {{");
            foreach (var assignment in state.createAssignments)
                builder.AppendLine($"            {assignment}");
            builder.AppendLine($"        }}");

            return builder.ToString();
        }
    }

    struct ManySystemsSystemAPI
    {
        public int idx;
        public Random r;
        public HashSet<string> fields;
        public HashSet<string> createAssignments;
        public bool generate;
    }

    static ManySystemsSystemAPI state;

    [MenuItem("DOTS/StressTests ManySystems/Generate SystemAPI C# Files")]
    private static void DOTS_StressTestsManySystems_GenerateSystemAPICSharpFiles()
    {
        state = new ManySystemsSystemAPI
        {
            r = new Random(272733), fields = new HashSet<string>(), createAssignments = new HashSet<string>()
        };

        const int countSystems = 50;
        using (var stream = new StreamWriter("Assets/StressTests/ManySystems_SystemAPI/BeforeGen/Source.cs"))
        {
//            state.idx = 0;
            stream.WriteLine("using Unity.Entities;");
            stream.WriteLine("using Unity.Transforms;");
            stream.WriteLine("using Unity.Burst;");
            stream.WriteLine("using static Unity.Entities.SystemAPI;");
            stream.WriteLine("namespace StressTests.ManySystems");
            stream.WriteLine("{");
            const string s = "ISystemAPIManyTestsBeforeGen";
            for (var i = 0; i < countSystems; ++i)
            {
                stream.WriteLine($"    [BurstCompile, DisableAutoCreation]");
                stream.WriteLine($"    partial struct {s}_{i:0000} : {s} ");
                stream.WriteLine($"    {{");
                stream.WriteLine($"{OnUpdate}");
                stream.WriteLine($"{OnCreate}");
                stream.WriteLine($"        public void OnDestroy(ref SystemState state){{ }}");
                stream.WriteLine($"    }}");

                state.fields.Clear();
                state.createAssignments.Clear();
            }
            stream.WriteLine("}");
        }

        state.generate = true;

        using (var stream = new StreamWriter("Assets/StressTests/ManySystems_SystemAPI/AfterGen/Source.cs"))
        {
//            state.idx = 0;
            stream.WriteLine("using Unity.Entities;");
            stream.WriteLine("using Unity.Transforms;");
            stream.WriteLine("using Unity.Burst;");
            stream.WriteLine("using static Unity.Entities.SystemAPI;");
            stream.WriteLine("namespace StressTests.ManySystems");
            stream.WriteLine("{");
            const string s = "ISystemAPIManyTestsAfterGen";
            for (var i = 0; i < countSystems; ++i)
            {
                stream.WriteLine($"    [BurstCompile, DisableAutoCreation]");
                stream.WriteLine($"    partial struct {s}_{i:0000} : {s} ");
                stream.WriteLine($"    {{");
                stream.WriteLine($"{OnUpdate}");
                stream.WriteLine($"{OnCreate}");
                stream.WriteLine($"        public void OnDestroy(ref SystemState state){{ }}");
                stream.WriteLine($"    }}");

                state.fields.Clear();
                state.createAssignments.Clear();
            }
            stream.WriteLine("}");
        }
    }
}
