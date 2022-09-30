using System.IO;
using Unity.Build;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class SetupGraphicsTestCases : IPrebuildSetup
{
    private static BuildTarget target;

    public void Setup()
    {
        // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
        // Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
        // can be used directly instead.
        //new UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup("Assets/ReferenceImages");

        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup("Assets/ReferenceImages");
    }
}
