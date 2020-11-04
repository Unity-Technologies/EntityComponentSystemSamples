using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Physics.Samples.Test
{
    // Runs all simulation types on the same cloned physics world for a
    // predefined number of steps and compares results.
    // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR
    [TestFixture]
#endif
    class UnityPhysicsSimulationDeterminismTest
    {
#if HAVOK_PHYSICS_EXISTS
        public bool SimulateHavok = false;
#endif
        static World DefaultWorld => World.DefaultGameObjectInjectionWorld;

        // Put the names of demos that shouldn't
        // be run in this test in this array
        private static string[] s_FilteredOutDemos =
        {
            "SingleThreadedRagdoll", "LoaderScene",
            "InitTestScene",

            // Following demos are removed from SimulationDeterminism because they take
            // too long to complete, and bring no special value
            "PlanetGravity", "LargeMesh", "Force Field", "ComplexStacking"
        };

        protected static IEnumerable GetScenes()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var scenes = new List<string>();
            for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                var shouldAdd = true;

                for (int i = 0; i < s_FilteredOutDemos.Length; i++)
                {
                    if (scenePath.Contains(s_FilteredOutDemos[i]))
                    {
                        shouldAdd = false;
                        break;
                    }
                }

                if (shouldAdd)
                {
                    scenes.Add(scenePath);
                }
            }
            scenes.Sort();
            return scenes;
        }

#if !UNITY_EDITOR
        [UnityTest]
        [Timeout(240000)]
#endif
        public virtual IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            // Wait for next frame
            yield return null;

            // Number of steps to simulate
            const int k_StopAfterStep = 100;

            // Number of worlds to simulate
            const int k_NumWorlds = 4;

            // Number of threads in each of the runs (3rd run is single threaded simulation)
            NativeArray<int> numThreadsPerRun = new NativeArray<int>(k_NumWorlds, Allocator.Persistent);
            numThreadsPerRun[0] = 8;
            numThreadsPerRun[1] = 4;
            numThreadsPerRun[2] = 0;
            numThreadsPerRun[3] = -1;

            // Load the scene and wait 2 frames
            SceneManager.LoadScene(scenePath);

            yield return null;
            yield return null;

            var sampler = DefaultWorld.GetOrCreateSystem<BuildPhysicsWorldSampler>();
            sampler.BeginSampling();

            while (!sampler.FinishedSampling)
            {
                yield return new WaitForSeconds(0.05f);
            }

            var buildPhysicsWorld = DefaultWorld.GetOrCreateSystem<BuildPhysicsWorld>();
            var stepComponent = PhysicsStep.Default;
            if (buildPhysicsWorld.HasSingleton<PhysicsStep>())
            {
                stepComponent = buildPhysicsWorld.GetSingleton<PhysicsStep>();
            }

            // Extract original world and make copies
            List<PhysicsWorld> physicsWorlds = new List<PhysicsWorld>(k_NumWorlds);
            physicsWorlds.Add(sampler.PhysicsWorld.Clone());

            physicsWorlds.Add(physicsWorlds[0].Clone());
            physicsWorlds.Add(physicsWorlds[1].Clone());
            physicsWorlds.Add(physicsWorlds[2].Clone());

            NativeArray<int> buildStaticTree = new NativeArray<int>(1, Allocator.Persistent);
            buildStaticTree[0] = 1;

            // Simulation step input
            var stepInput = new SimulationStepInput()
            {
                Gravity = stepComponent.Gravity,
                NumSolverIterations = stepComponent.SolverIterationCount,
                SolverStabilizationHeuristicSettings = stepComponent.SolverStabilizationHeuristicSettings,
                SynchronizeCollisionWorld = true,
                TimeStep = DefaultWorld.Time.DeltaTime
            };

            // Step the simulation on all worlds
            for (int i = 0; i < physicsWorlds.Count; i++)
            {
                int threadCountHint = numThreadsPerRun[i];
                if (threadCountHint == -1)
                {
                    stepInput.World = physicsWorlds[i];
                    stepInput.World.CollisionWorld.BuildBroadphase(
                        ref stepInput.World, stepInput.TimeStep, stepInput.Gravity, true);

#if HAVOK_PHYSICS_EXISTS
                    if (SimulateHavok)
                    {
                        var simulationContext = new Havok.Physics.SimulationContext(Havok.Physics.HavokConfiguration.Default);
                        for (int step = 0; step < k_StopAfterStep; step++)
                        {
                            simulationContext.Reset(ref stepInput.World);
                            new StepHavokJob
                            {
                                Input = stepInput,
                                SimulationContext = simulationContext
                            }.Schedule().Complete();
                        }

                        simulationContext.Dispose();
                    }
                    else
#endif
                    {
                        var simulationContext = new SimulationContext();
                        for (int step = 0; step < k_StopAfterStep; step++)
                        {
                            simulationContext.Reset(stepInput);
                            new StepJob
                            {
                                Input = stepInput,
                                SimulationContext = simulationContext
                            }.Schedule().Complete();
                        }

                        simulationContext.Dispose();
                    }
                }
                else
                {
#if HAVOK_PHYSICS_EXISTS
                    if (SimulateHavok)
                    {
                        var simulation = new Havok.Physics.HavokSimulation(Havok.Physics.HavokConfiguration.Default);
                        stepInput.World = physicsWorlds[i];
                        stepInput.World.CollisionWorld.ScheduleBuildBroadphaseJobs(
                            ref stepInput.World, stepInput.TimeStep, stepInput.Gravity, buildStaticTree, default, threadCountHint).Complete();
                        for (int step = 0; step < k_StopAfterStep; step++)
                        {
                            var handles = simulation.ScheduleStepJobs(stepInput, null, default, threadCountHint);
                            handles.FinalExecutionHandle.Complete();
                            handles.FinalDisposeHandle.Complete();
                        }
                        simulation.Dispose();
                    }
                    else
#endif
                    {
                        var simulation = new Simulation();
                        stepInput.World = physicsWorlds[i];
                        stepInput.World.CollisionWorld.ScheduleBuildBroadphaseJobs(
                            ref stepInput.World, stepInput.TimeStep, stepInput.Gravity, buildStaticTree, default, threadCountHint).Complete();
                        for (int step = 0; step < k_StopAfterStep; step++)
                        {
                            var handles = simulation.ScheduleStepJobs(stepInput, null, default, threadCountHint);
                            handles.FinalExecutionHandle.Complete();
                            handles.FinalDisposeHandle.Complete();
                        }
                        simulation.Dispose();
                    }
                }
            }

            // Verify simulation results
            for (int i = 0; i < physicsWorlds.Count - 1; i++)
            {
                for (int j = i + 1; j < physicsWorlds.Count; j++)
                {
                    var world1 = physicsWorlds[i];
                    var world2 = physicsWorlds[j];
                    for (int k = 0; k < world1.NumBodies; k++)
                    {
                        var result1 = world1.Bodies[k].WorldFromBody;
                        var result2 = world2.Bodies[k].WorldFromBody;
                        if (!math.all(result1.pos == result2.pos))
                        {
                            Debug.Log($"{i} vs {j}: Expected: {result1.pos}, Actual: {result2.pos}");
                        }
                        if (!math.all(result1.rot.value == result2.rot.value))
                        {
                            Debug.Log($"{i} vs {j}: Expected: {result1.rot.value}, Actual: {result2.rot.value}");
                        }
                    }
                }
            }

            // Clean up
            {
                SwitchWorlds();
                numThreadsPerRun.Dispose();
                buildStaticTree.Dispose();
                for (int i = 0; i < physicsWorlds.Count; i++)
                {
                    physicsWorlds[i].Dispose();
                }
                LogAssert.NoUnexpectedReceived();
            }
        }

        [TearDown]
        public void TearDown()
        {
            SwitchWorlds();
        }

        protected static void SwitchWorlds()
        {
            var entityManager = DefaultWorld.EntityManager;
            entityManager.CompleteAllJobs();
            var entities = entityManager.GetAllEntities();
            entityManager.DestroyEntity(entities);
            entities.Dispose();

            foreach (var system in DefaultWorld.Systems)
            {
                system.Enabled = false;
            }
            DefaultWorld.Dispose();
            DefaultWorldInitialization.Initialize("Default World", false);
        }

        [Burst.BurstCompile]
        internal struct StepJob : IJob
        {
            public SimulationStepInput Input;
            public SimulationContext SimulationContext;

            public void Execute()
            {
                Simulation.StepImmediate(Input, ref SimulationContext);
            }
        }

        [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
        [UpdateAfter(typeof(BuildPhysicsWorld))]
        class BuildPhysicsWorldSampler : SystemBase
        {
            public bool FinishedSampling = false;
            public World DefaultWorld => World.DefaultGameObjectInjectionWorld;
            public PhysicsWorld PhysicsWorld;

            public BuildPhysicsWorld BuildPhysicWorldSystem;

            public void BeginSampling()
            {
                Enabled = true;
            }

            protected override void OnCreate()
            {
                Enabled = false;
                PhysicsWorld = new PhysicsWorld(0, 0, 0);
                BuildPhysicWorldSystem = DefaultWorld.GetOrCreateSystem<BuildPhysicsWorld>();
            }

            protected override void OnUpdate()
            {
                BuildPhysicWorldSystem.GetOutputDependency().Complete();
                if (BuildPhysicWorldSystem.PhysicsWorld.NumBodies != 0)
                {
                    EntityManager.CompleteAllJobs();
                    PhysicsWorld.Dispose();
                    PhysicsWorld = BuildPhysicWorldSystem.PhysicsWorld.Clone();
                    Enabled = false;
                    FinishedSampling = true;
                }
            }

            protected override void OnDestroy()
            {
                PhysicsWorld.Dispose();
            }
        }

#if HAVOK_PHYSICS_EXISTS
        [Burst.BurstCompile]
        internal struct StepHavokJob : IJob
        {
            public SimulationStepInput Input;
            public Havok.Physics.SimulationContext SimulationContext;

            public void Execute()
            {
                Havok.Physics.HavokSimulation.StepImmediate(Input, ref SimulationContext);
            }
        }
#endif
    }

#if HAVOK_PHYSICS_EXISTS
#if !UNITY_EDITOR
    [TestFixture]
#endif
    class HavokPhysicsSimulationDeterminismTest : UnityPhysicsSimulationDeterminismTest
    {
        public HavokPhysicsSimulationDeterminismTest()
        {
            SimulateHavok = true;
        }
    }
#endif
}
