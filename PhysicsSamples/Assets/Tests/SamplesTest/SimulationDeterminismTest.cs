#if UNITY_ANDROID && !UNITY_64
#define UNITY_ANDROID_ARM7V
#endif

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

namespace Unity.Physics.Tests
{
    // Runs all simulation types on the same cloned physics world for a
    // predefined number of steps and compares results.
    // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR || UNITY_PHYSICS_INCLUDE_END2END_TESTS
    [TestFixture]
#endif
    partial class UnityPhysicsSimulationDeterminismTest
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
            "PlanetGravity", "LargeMesh", "Force Field", "ComplexStacking",

            // Removed as long as Havok plugins are not build with -strict floating point mode
            "RaycastCar", "Joints - Parade",

            // These demos do some verifications that would currently fail with UP
            "AllMotors.unity",

#if UNITY_ANDROID_ARM7V
            // disabled due to sigbuss crashes, something is defo corrupting memory in the allocators
            "CharacterController",
            "Animation",
            "ClientServer",
            "DeactivatedBodiesTriggerTest",
#endif
#if !(UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
            "SimpleStacking", //disabled due to being unstable on some console devices
#endif
#if UNITY_GAMECORE
            "SoftJoint" // disabled on Xbox Series X as it fails.  Jira ticket: DOTS-6520
#endif
#if UNITY_IOS
            // Scences disabled on iOS. Bug report: DOTS-9614
            "Joints - Ragdolls",
            "ChangeGroundFilter",
            "ChangeGroundFilterChangeCollider",
            "ChangeGroundFilterChangeMotionType",
            "ChangeGroundFilterNewCollider",
            "ChangeGroundFilterRemove",
            "ChangeGroundFilterTeleport",
            "CollisionResponse.None",
            "ChangeCompoundFilter",
            "Compound",
            "FixedAngleGrid",
            "InvalidJoint",
            "RagdollGrid",
            "SoftJoint",
            "SingleThreadedRagdoll",
            "Terrain_Triangles",
            "Terrain_VertexSamples"
#endif
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

#if !UNITY_EDITOR || UNITY_PHYSICS_INCLUDE_END2END_TESTS
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
            const int k_NumWorlds = 3;

            // Number of threads in each of the runs (2nd run is immediate mode simulation)
            NativeArray<int> numThreadsPerRun = new NativeArray<int>(k_NumWorlds, Allocator.Persistent);
            numThreadsPerRun[0] = 4;
            numThreadsPerRun[1] = 0;
            numThreadsPerRun[2] = -1;

            // Load the scene and wait 2 frames
            SceneManager.LoadScene(scenePath);

            yield return null;
            yield return null;

            var sampler = DefaultWorld.GetOrCreateSystemManaged<BuildPhysicsWorldSampler>();
            sampler.BeginSampling();

            while (!sampler.FinishedSampling)
            {
                yield return new WaitForSeconds(0.05f);
            }

            var stepComponent = PhysicsStep.Default;
            using (var query = DefaultWorld.EntityManager.CreateEntityQuery(typeof(PhysicsStep)))
            {
                if (query.HasSingleton<PhysicsStep>())
                {
                    stepComponent = query.GetSingleton<PhysicsStep>();
                }
            }

            // Extract original world and make copies
            List<PhysicsWorld> physicsWorlds = new List<PhysicsWorld>(k_NumWorlds);
            for (int i = 0; i < k_NumWorlds; i++)
            {
                if (i == 0)
                {
                    physicsWorlds.Add(sampler.PhysicsWorld.Clone());
                }
                else
                {
                    physicsWorlds.Add(physicsWorlds[0].Clone());
                }
            }

            var buildStaticTree = new NativeReference<int>(1, Allocator.Persistent);

            // Simulation step input
            var stepInput = new SimulationStepInput()
            {
                Gravity = stepComponent.Gravity,
                NumSolverIterations = stepComponent.SolverIterationCount,
                SolverStabilizationHeuristicSettings = stepComponent.SolverStabilizationHeuristicSettings,
                SynchronizeCollisionWorld = true,
                TimeStep = DefaultWorld.Time.DeltaTime,
                HaveStaticBodiesChanged = buildStaticTree,
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
                    bool multiThreaded = threadCountHint > 0 ? true : false;
#if HAVOK_PHYSICS_EXISTS
                    if (SimulateHavok)
                    {
                        var simulation = new Havok.Physics.HavokSimulation(Havok.Physics.HavokConfiguration.Default);
                        stepInput.World = physicsWorlds[i];
                        stepInput.World.CollisionWorld.ScheduleBuildBroadphaseJobs(
                            ref stepInput.World, stepInput.TimeStep, stepInput.Gravity, buildStaticTree, default, multiThreaded).Complete();
                        for (int step = 0; step < k_StopAfterStep; step++)
                        {
                            var handles = new SimulationJobHandles(new JobHandle());
                            handles = simulation.ScheduleStepJobs(stepInput, default, multiThreaded);
                            handles.FinalExecutionHandle.Complete();
                            handles.FinalDisposeHandle.Complete();
                        }
                        simulation.Dispose();
                    }
                    else
#endif
                    {
                        var simulation = Simulation.Create();
                        stepInput.World = physicsWorlds[i];
                        stepInput.World.CollisionWorld.ScheduleBuildBroadphaseJobs(
                            ref stepInput.World, stepInput.TimeStep, stepInput.Gravity, buildStaticTree, default, multiThreaded).Complete();
                        for (int step = 0; step < k_StopAfterStep; step++)
                        {
                            var handles = new SimulationJobHandles(new JobHandle());

                            handles = simulation.ScheduleStepJobs(stepInput, default, multiThreaded);
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
                VerifyConsoleMessages.VerifyPrintedMessages(scenePath);
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
            entityManager.CompleteAllTrackedJobs();
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
        [UpdateAfter(typeof(PhysicsSystemGroup))]
        partial class BuildPhysicsWorldSampler : SystemBase
        {
            public bool FinishedSampling = false;
            public World DefaultWorld => World.DefaultGameObjectInjectionWorld;
            public PhysicsWorld PhysicsWorld;
            public EntityQuery PhysicsWorldSingletonQuery;

            public void BeginSampling()
            {
                Enabled = true;
            }

            protected override void OnCreate()
            {
                Enabled = false;
                PhysicsWorld = new PhysicsWorld(0, 0, 0);
                PhysicsWorldSingletonQuery = GetEntityQuery(
                    new EntityQueryBuilder(Allocator.Temp).WithAll<PhysicsWorldSingleton>());
            }

            protected override void OnUpdate()
            {
                CompleteDependency();
                var world = PhysicsWorldSingletonQuery.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

                if (world.NumBodies != 0)
                {
                    EntityManager.CompleteAllTrackedJobs();
                    PhysicsWorld.Dispose();
                    PhysicsWorld = world.Clone();
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
#if !UNITY_EDITOR || UNITY_PHYSICS_INCLUDE_END2END_TESTS
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
