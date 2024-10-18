#if UNITY_EDITOR_WIN && HAVOK_PHYSICS_EXISTS

using Havok.Physics;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Tests;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Generics = System.Collections.Generic;

// Class for base Havok Visual Debugger tests. It just loads the the Hello World scene and ensures that Havok physics is used.
class HavokPhysicsVDBTest : UnityPhysicsSamplesTest
{
    protected static IEnumerable GetVDBScenes()
    {
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        var scenes = new Generics.List<string>();
        for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            if (scenePath.Contains("Hello World"))
            {
                scenes.Add(scenePath);
            }
        }
        return scenes;
    }

    private IEnumerator SetupAndLoadScene(World world, HavokConfiguration havokConfig, string scenePath)
    {
        // Ensure Havok simulation
        ConfigureSimulation(world, SimulationType.HavokPhysics);

        var system = world.GetOrCreateSystemManaged<SimulationConfigurationSystem>();
        system.EntityManager.CreateEntity(typeof(HavokConfiguration));
        var query = new EntityQueryBuilder(system.WorldUpdateAllocator).WithAllRW<HavokConfiguration>().Build(system);
        query.SetSingleton(havokConfig);

        SceneManager.LoadScene(scenePath);
        yield return new WaitForSeconds(1);
        ResetDefaultWorld();
        yield return new WaitForFixedUpdate();
    }

    [UnityTest]
    [Timeout(240000)]
    public IEnumerator LoadScenes([ValueSource(nameof(GetVDBScenes))] string scenePath)
    {
        VerifyConsoleMessages.ClearMessagesInConsole();

        var vdbProcess = new System.Diagnostics.Process();

        // Close any existing instances of the VDB and make a new one
        {
            string vdbExe = System.IO.Path.GetFullPath("Packages/com.havok.physics/Tools/VisualDebugger/HavokVisualDebugger.exe");
            string vdbProcessName = System.IO.Path.GetFileNameWithoutExtension(vdbExe);

            List<System.Diagnostics.Process> processes = new List<System.Diagnostics.Process>();
            processes.AddRange(System.Diagnostics.Process.GetProcessesByName(vdbProcessName));
            foreach (var process in processes)
            {
                process.CloseMainWindow();
                process.Close();
            }

            vdbProcess.StartInfo.FileName = vdbExe;
            vdbProcess.StartInfo.Arguments = "";
            vdbProcess.Start();
            vdbProcess.WaitForInputIdle();
            // How do we ensure the VDB is ready to connect?
            yield return new WaitForSeconds(2);
            vdbProcess.Refresh();
        }

        var havokConfig = HavokConfiguration.Default;

        // Enabled VDB
        havokConfig.VisualDebugger.Enable = 1;
        yield return SetupAndLoadScene(World.DefaultGameObjectInjectionWorld, havokConfig, scenePath);

        // Disabled VDB
        havokConfig.VisualDebugger.Enable = 0;
        yield return SetupAndLoadScene(World.DefaultGameObjectInjectionWorld, havokConfig, scenePath);

        // Enabled VDB with zero Timer memory
        havokConfig.VisualDebugger.Enable = 1;
        havokConfig.VisualDebugger.TimerBytesPerThread = 0;
        yield return SetupAndLoadScene(World.DefaultGameObjectInjectionWorld, havokConfig, scenePath);

        // Close VDB client
        vdbProcess.CloseMainWindow();
        vdbProcess.Close();

        // Enabled VDB with no Client running.
        havokConfig = HavokConfiguration.Default;
        havokConfig.VisualDebugger.Enable = 1;
        yield return SetupAndLoadScene(World.DefaultGameObjectInjectionWorld, havokConfig, scenePath);

        VerifyConsoleMessages.VerifyPrintedMessages(scenePath);
    }
}

#endif
