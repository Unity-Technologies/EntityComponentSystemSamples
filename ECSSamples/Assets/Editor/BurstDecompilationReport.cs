using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;
using UnityEngine;

////
// This is a temporary solution until burst get necessary APIs exposed
// https://github.cds.internal.unity3d.com/unity/burst/issues/579
////
class BurstDecompilationReport
{
    static Regex m_MysteryCharacterRegex = new Regex(@"\n.*\z");
    internal enum DisassemblyKind
    {
        Asm = 0,
        IL = 1,
        UnoptimizedIR = 2,
        OptimizedIR = 3,
        IRPassAnalysis = 4
    }

    private static readonly string[] CodeGenOptions =
    {
        "auto",
        "x86_sse2",
        "x86_sse4",
        "x64_sse2",
        "x64_sse4",
        "avx",
        "avx2",
        "avx512",
        "armv7a_neon32",
        "armv8a_aarch64",
        "thumb2_neon32",
    };

    private static readonly string[] DisasmOptions =
    {
        "\n--dump=Asm",
        "\n--dump=IL",
        "\n--dump=IR",
        "\n--dump=IROptimized",
        "\n--dump=IRPassAnalysis"
    };

    public static void DumpBurstInspectorReport()
    {
        DumpBurstInspectorReportToFolder(null);
    }

    public static void DumpBurstInspectorReportToFolder(string outputFolder)
    {
        if (string.IsNullOrEmpty(outputFolder))
        {
            outputFolder = "BurstReport";
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            Directory.CreateDirectory(outputFolder);
        }

        Debug.Log("Generating Burst Disassembly Report to folder " + outputFolder);

        try
        {
            var burstCompileTargetType = Type.GetType("Unity.Burst.Editor.BurstCompileTarget, Unity.Burst.Editor");
            var burstCompilerServiceType = Type.GetType("Unity.Burst.LowLevel.BurstCompilerService, UnityEngine.CoreModule");
            var burstReflectionType = Type.GetType("Unity.Burst.Editor.BurstReflection, Unity.Burst.Editor");
            var findExecuteMethodsResultType = burstReflectionType.GetNestedType("FindExecuteMethodsResult");
            var compileTargetsField = findExecuteMethodsResultType.GetField("CompileTargets");
            var findExecuteMethodsMethod = burstReflectionType.GetMethod("FindExecuteMethods", BindingFlags.Public | BindingFlags.Static);
            var burstReflectionAssemblyOptionsType = Type.GetType("Unity.Burst.Editor.BurstReflectionAssemblyOptions, Unity.Burst.Editor");
            var editorAssembliesThatCanPossiblyContainJobsProperty = burstReflectionType.GetField("EditorAssembliesThatCanPossiblyContainJobs", BindingFlags.Public | BindingFlags.Static);
            var getDisassemblyMethod = burstCompilerServiceType.GetMethod("GetDisassembly", BindingFlags.Public | BindingFlags.Static);
            var getDisplayNameMethod = burstCompileTargetType.GetMethod("GetDisplayName");

            var disasmOption = DisasmOptions[0];
            var codeGenOptions = CodeGenOptions[0];
            var baseOptions = "--enable-synchronous-compilation\n--fastmath\n--target=" + codeGenOptions;

            var burstReflectionAssemblyOptions = Enum.Parse(burstReflectionAssemblyOptionsType, "None");
            var assemblyList = editorAssembliesThatCanPossiblyContainJobsProperty.GetValue(null);

            var findExecuteMethodsResult = findExecuteMethodsMethod.Invoke(null, new[] {  assemblyList, burstReflectionAssemblyOptions });
            var targets = (IEnumerable<object>)compileTargetsField.GetValue(findExecuteMethodsResult);

            foreach (var target in targets)
            {
                if (!HasRequiredBurstCompileAttributes())
                    continue;

                var targetMethod = (MethodInfo)burstCompileTargetType.GetField("Method").GetValue(target);
                var name = (string)getDisplayNameMethod.Invoke(target, null);

                var dir = string.Join("_", outputFolder.Split(Path.GetInvalidPathChars()));
                var fileName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

                // This seems to be maximum file name length accepted on windows
                if (fileName.Length > (199 - ".txt".Length))
                {
                    fileName = fileName.GetHashCode() + "-" + fileName.Substring(fileName.Length - 180, 180);
                }

                fileName += ".txt";
                var path = Path.Combine(dir, fileName);
                var result = (string)getDisassemblyMethod.Invoke(null, new object[] { targetMethod, baseOptions + disasmOption });
                result = m_MysteryCharacterRegex.Replace(result, "\n");
                File.WriteAllText(path, result);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            File.WriteAllText(Path.Combine(outputFolder, "exception.txt"), e.ToString());
        }

        if (!string.IsNullOrEmpty(outputFolder))
        {
            Debug.Log("Saved BurstInspectorReport in " + outputFolder);
        }
    }

    public static bool HasRequiredBurstCompileAttributes()
    {
        //TODO implement this
//        return BurstCompilerOptions.HasBurstCompileAttribute(JobType) && (!IsStaticMethod || BurstCompilerOptions.HasBurstCompileAttribute(Method))
        return true;
    }
}
