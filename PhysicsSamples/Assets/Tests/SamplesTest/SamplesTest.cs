#if UNITY_ANDROID && !UNITY_64
#define UNITY_ANDROID_ARM7V
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Physics.Systems;
using Unity.Scenes;

namespace Unity.Physics.Tests
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    partial class SimulationConfigurationSystem : SystemBase
    {
        public SimulationType SimulationType;
        public int NumSubsteps;
        public int NumSolverIterations;
        public bool MultiThreaded;
        public bool IncrementalDynamicBroadphase;
        public bool IncrementalStaticBroadphase;
        public bool SubstepOverride;
        public bool SolverIterationOverride;

        protected override void OnUpdate()
        {
            if (SystemAPI.HasSingleton<PhysicsStep>())
            {
                var component = SystemAPI.GetSingletonRW<PhysicsStep>();
                component.ValueRW.SimulationType = SimulationType;
                component.ValueRW.MultiThreaded = (byte)(MultiThreaded ? 1 : 0);
                component.ValueRW.IncrementalDynamicBroadphase = IncrementalDynamicBroadphase;
                component.ValueRW.IncrementalStaticBroadphase = IncrementalStaticBroadphase;
                if (SubstepOverride) component.ValueRW.SubstepCount = NumSubsteps;
                if (SolverIterationOverride) component.ValueRW.SolverIterationCount = NumSolverIterations;
            }
            else
            {
                CompleteDependency();
            }
        }
    }

    [TestFixture]
    abstract class UnityPhysicsSamplesTest
    {
        protected static World DefaultWorld => World.DefaultGameObjectInjectionWorld;

        protected static IEnumerable GetScenes()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var scenes = new List<string>();
            for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                if (scenePath.Contains("InitTestScene")
                    || scenePath.Contains("Built-in Prefab Joint Conversion")
                    || scenePath.Contains("ChainTestWithMass") // Test runs for 26s. Only want for performance testing
                                                               // in order to circumvent API breakages that do not affect physics, some packages are removed from the project on CI
                                                               // any scenes referencing asset types in com.unity.inputsystem must be guarded behind UNITY_INPUT_SYSTEM_EXISTS
#if !UNITY_INPUT_SYSTEM_EXISTS
                    || scenePath.Contains("LoaderScene")
#endif
                )
                    continue;

#if UNITY_ANDROID_ARM7V || UNITY_IOS
                // Terrain scene needs a lot of memory, skip it on Android armv7 and IOS
                if (scenePath.Contains("/Terrain.unity"))
                    continue;

                // Performance scenes need a lot of memory, skip it on Android armv7 and IOS
                if (scenePath.Contains("/ConvexCollisionPerformanceTest.unity") ||
                    scenePath.Contains("/CubeCollisionPerformanceTest.unity") ||
                    scenePath.Contains("/RagdollPerformanceTest.unity") ||
                    scenePath.Contains("/SphereCollisionPerformanceTest.unity"))
                    continue;

                // Seems to run out of memory on armv7 and IOS
                if (scenePath.Contains("/Character Controller.unity") ||
                    scenePath.Contains("/Raycast Car.unity"))
                    continue;

                //SIGSEGV/SIGBUSS error looks like there's some alignment/out of bounds access somewhere
                //Sample tests seem to randomly trigger it on CI, at the moment of this comment it is not reproducible locally
                if (scenePath.Contains("/Animation.unity")
                    || scenePath.Contains("/ClientServer.unity")
                    || scenePath.Contains("/DeactivatedBodiesTriggerTest")) //all trigger test scenes
                    continue;
#endif

#if UNITY_IOS
                // Disabled due to iOS device specific crash on 2023.3.0a17: DOTS-9820
                if (scenePath.Contains("/Pyramids.unity"))
                    continue;

                // Tests we're skipping with HavokPhysics
                if (scenePath.Contains("/Joints - Ragdolls.unity") ||
                    scenePath.Contains("/ChangeGroundFilter.unity") ||
                    scenePath.Contains("/ChangeGroundFilterChangeCollider.unity") ||
                    scenePath.Contains("/ChangeGroundFilterChangeMotionType.unity") ||
                    scenePath.Contains("/ChangeGroundFilterNewCollider.unity") ||
                    scenePath.Contains("/ChangeGroundFilterRemove.unity") ||
                    scenePath.Contains("/ChangeGroundFilterTeleport.unity") ||
                    scenePath.Contains("/CollisionResponse.None.unity") ||
                    scenePath.Contains("/ChangeCompoundFilter.unity") ||
                    scenePath.Contains("/Compound.unity") ||
                    scenePath.Contains("/FixedAngleGrid.unity") ||
                    scenePath.Contains("/InvalidJoint.unity") ||
                    scenePath.Contains("/RagdollGrid.unity") ||
                    scenePath.Contains("/SoftJoint.unity") ||
                    scenePath.Contains("/SingleThreadedRagdoll.unity") ||
                    scenePath.Contains("/Terrain_Triangles.unity") ||
                    scenePath.Contains("/Terrain_VertexSamples.unity"))
                    continue;
#endif

#if UNITY_STANDALONE_WIN
                // DOTS-10318 RagdollPerformanceTest is failing on Windows Standalone
                if (scenePath.Contains("/RagdollPerformanceTest.unity"))
                    continue;
#endif

#if UNITY_STANDALONE_LINUX 
             // Tests we're skipping due to failure in Ubuntu for 1.4 release
             if (scenePath.Contains("/VehicleOverTerrain.unity"))
             {
                 continue;
             }
#endif                   

                scenes.Add(scenePath);
            }
            scenes.Sort();
            return scenes;
        }

        // Adds the specified scenes to the list of scenes to be tested rather than skipping the named scenes.
        protected static IEnumerable GetScenesForSubstepTesting()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var scenes = new List<string>();
            for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);

#if UNITY_IOS
                // Disabled due to iOS device specific crash on 2023.3.0a17: DOTS-9820
                if (scenePath.Contains("/Pyramids.unity"))
                    continue;
#endif
                // A selection of Demos & Tests where substepping should be tested. Did not select tests that are
                // conditionally skipped on some platforms (except for Pyramids on iOS)
                // Note: simulation validation fails for: Motion Properties - Mass.unity, BasicStacks.unity
                if (scenePath.Contains("/HelloWorld.unity") ||
                    scenePath.Contains("/GravityWell.unity") ||
                    scenePath.Contains("/Collider Parade - Basic.unity") ||
                    scenePath.Contains("/Motion Properties - Gravity Factor.unity") ||
                    scenePath.Contains("/Motion Properties - Velocity.unity") ||
                    scenePath.Contains("/Material Properties - Friction.unity") ||
                    scenePath.Contains("/Material Properties - Restitution.unity") ||
                    scenePath.Contains("/Events - Contacts.unity") ||
                    scenePath.Contains("/Events - Triggers - Gravity Factor.unity") ||
                    scenePath.Contains("/Joints - Parade.unity") ||
                    scenePath.Contains("/Motors - Parade.unity") ||
                    scenePath.Contains("/Joints - Single Ragdoll.unity") ||
                    scenePath.Contains("/Modify - Apply Impulse.unity") ||
                    scenePath.Contains("/Modify - Velocity.unity") ||
                    scenePath.Contains("/Modify - Kinematic Motion.unity") ||
                    scenePath.Contains("/Modify - Narrowphase Contacts.unity") ||
                    scenePath.Contains("/Modify - Surface Velocity.unity") ||
                    scenePath.Contains("/Pool.unity") ||

                    // A selection of Test scenes where substepping should be tested
                    scenePath.Contains("/CollisionUT.unity") ||
                    scenePath.Contains("/CollisionEventDataUT.unity") ||
                    scenePath.Contains("/CollisionEventsUT.unity") ||
                    scenePath.Contains("/RestitutionCollisionEventDataUT.unity") ||
                    scenePath.Contains("/TriggerEventDataUT.unity") ||
                    scenePath.Contains("/TriggerEventsUT.unity") ||
                    scenePath.Contains("/FrictionUT.unity") ||
                    scenePath.Contains("/RestitutionUT.unity") ||
                    scenePath.Contains("/ContactModifiersUT.unity") ||
                    scenePath.Contains("/JacobianModifiersUT.unity") ||
                    scenePath.Contains("/GravityFactorUT.unity") ||
                    scenePath.Contains("/ThinBoxes.unity") ||
                    scenePath.Contains("/Stiff Limits.unity") ||
                    scenePath.Contains("/Pyramids.unity") ||
                    scenePath.Contains("/SimpleStacking.unity"))
                {
                    scenes.Add(scenePath);
                }
            }
            scenes.Sort();
            return scenes;
        }

        [TearDown]
        public void TearDown()
        {
            ResetDefaultWorld();
        }

        protected IEnumerator LoadSceneAndSimulate(string scenePath)
        {
            VerifyConsoleMessages.ClearMessagesInConsole();

            yield return LoadScene(scenePath);

            yield return Simulate();

            ResetDefaultWorld();
            yield return new WaitForFixedUpdate();

            VerifyConsoleMessages.VerifyPrintedMessages(scenePath);
        }

        protected IEnumerator LoadScene(string scenePath)
        {
            SceneManager.LoadScene(scenePath);
            // Skip a frame in order to trigger loading so that the Sub Scene loading process is started and we can find
            // the corresponding scene entities below.
            yield return new WaitForFixedUpdate();

            // Find all Sub Scenes and make sure they are loaded before proceeding
            using (var subSceneQuery = DefaultWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SceneReference>()))
            {
                using (var sceneEntities = subSceneQuery.ToEntityArray(Allocator.Persistent))
                {
                    bool loading = false;
                    do
                    {
                        loading = false;
                        foreach (var sceneEntity in sceneEntities)
                        {
                            if (!SceneSystem.IsSceneLoaded(DefaultWorld.Unmanaged, sceneEntity))
                            {
                                loading = true;
                                break;
                            }
                        }

                        // keep waiting by skipping a frame while Sub Scenes are still being loaded
                        if (loading)
                        {
                            yield return new WaitForFixedUpdate();
                        }
                    }
                    while (loading);
                }
            }
        }

        protected IEnumerator Simulate()
        {
            // Find Simulation Validation in the loaded scene and enable validation if present.
            // Then run the simulation until the end period specified in the Simulation Validation Settings, unless
            // it's set to "infinity" (value < 0, see SimulationValidationAuthoring).
            var simulationTime = 1.0f;
            using (var query = DefaultWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<SimulationValidationSettings>()))
            {
                if (query.TryGetSingletonRW(out RefRW<SimulationValidationSettings> validationSettings))
                {
                    validationSettings.ValueRW.EnableValidation = true;
                    var timeRange = validationSettings.ValueRO.ValidationTimeRange;
                    // obtain simulation end time unless it's set to "infinity"
                    if (timeRange[1] >= 0)
                    {
                        simulationTime = timeRange[1];
                    }
                    else
                    {
                        // if infinite simulation validation is requested (timeRange[1] < 0),
                        // simulate at least as long as required to start the validation plus one extra second.
                        simulationTime = timeRange[0] + 1;
                    }

                    var msg = $"Performing simulation validation: test duration set to {simulationTime} seconds.";
                    Debug.Log(msg);
                    LogAssert.Expect(LogType.Log, msg);
                }
            }

            yield return new WaitForSeconds(simulationTime);
        }

        protected static void ResetDefaultWorld()
        {
            if (DefaultWorld.IsCreated)
            {
                var systems = DefaultWorld.Systems;
                foreach (var s in systems)
                {
                    s.Enabled = false;
                }
                DefaultWorld.Dispose();
            }

            DefaultWorldInitialization.Initialize("Default World", false);
        }

        protected static void ConfigureSimulation(in World world, in SimulationType simulationType, in bool multiThreaded = true,
            in bool incrementalDynamicBroadphase = false, in bool incrementalStaticBroadphase = false,
            in int numSubsteps = 1, in int numSolverIterations = 4, in bool substepOverride = false, in bool solverIterationOverride = false)
        {
            var configSystem = world.GetExistingSystemManaged<SimulationConfigurationSystem>();

            Assert.IsNull(configSystem,
                $"The '{nameof(SimulationConfigurationSystem)}' system should only be created by the '{nameof(ConfigureSimulation)}' function!");

            configSystem = new SimulationConfigurationSystem
            {
                SimulationType = simulationType,
                MultiThreaded = multiThreaded,
                IncrementalDynamicBroadphase = incrementalDynamicBroadphase,
                IncrementalStaticBroadphase = incrementalStaticBroadphase,
                NumSubsteps = numSubsteps,
                NumSolverIterations = numSolverIterations,
                SubstepOverride = substepOverride,
                SolverIterationOverride = solverIterationOverride
            };
            world.AddSystemManaged(configSystem);
            world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().AddSystemToUpdateList(configSystem);
        }
    }

    [TestFixture]
    class UnityPhysicsSamplesTestMT : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(240000)]
        public IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            // Enable multi threaded Unity Physics simulation
            ConfigureSimulation(DefaultWorld, SimulationType.UnityPhysics);

            yield return LoadSceneAndSimulate(scenePath);
        }
    }

    [TestFixture]
    class UnityPhysicsSamplesTestST : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(240000)]
        public IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            // Enable single threaded Unity Physics simulation
            ConfigureSimulation(DefaultWorld, SimulationType.UnityPhysics, false);

            yield return LoadSceneAndSimulate(scenePath);
        }
    }

    class UnityPhysicsSamplesTestST_Substep4_Solve2 : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(240000)]
        public IEnumerator LoadScenes([ValueSource(nameof(GetScenesForSubstepTesting))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            var numSolverIterations = 2;
            if (scenePath.Contains("/ThinBoxes.unity"))
            {
                numSolverIterations = 4;
            }

            // Enable single threaded Unity Physics simulation
            ConfigureSimulation(DefaultWorld, SimulationType.UnityPhysics, false,
                false, false, 4, numSolverIterations, true, true);

            yield return LoadSceneAndSimulate(scenePath);
        }
    }

    class UnityPhysicsSamplesTestMT_Substep4_Solve2 : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(240000)]
        public IEnumerator LoadScenes([ValueSource(nameof(GetScenesForSubstepTesting))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            var numSolverIterations = 2;
            if (scenePath.Contains("/ThinBoxes.unity"))
            {
                numSolverIterations = 4;
            }

            // Enable single threaded Unity Physics simulation
            ConfigureSimulation(DefaultWorld, SimulationType.UnityPhysics, true,
                false, false, 4, numSolverIterations, true, true);

            yield return LoadSceneAndSimulate(scenePath);
        }
    }
}
