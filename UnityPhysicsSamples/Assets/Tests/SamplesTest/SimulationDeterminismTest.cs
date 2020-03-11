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
    class SimulationDeterminismTest
    {
        static World DefaultWorld => World.DefaultGameObjectInjectionWorld;

        protected static IEnumerable GetScenes()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var scenes = new List<string>();
            for (int sceneIndex = 0; sceneIndex < sceneCount; ++sceneIndex)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
                if (scenePath.Contains("InitTestScene"))
                    continue;

                scenes.Add(scenePath);
            }
            scenes.Sort();
            return scenes;
        }

#if !UNITY_EDITOR
        [UnityTest]
        [Timeout(60000)]
#endif
        public virtual IEnumerator LoadScenes([ValueSource(nameof(GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

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

            // Load the scene and wait a while
            SceneManager.LoadScene(scenePath);

            // Finalize the current build of physics world and wait for bodies to appear in the physics world
            var buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BuildPhysicsWorld>();
            buildPhysicsWorld.FinalJobHandle.Complete();
            while (buildPhysicsWorld.PhysicsWorld.NumBodies == 0)
            {
                yield return new WaitForSeconds(0.01f);
                buildPhysicsWorld.FinalJobHandle.Complete();
            }

            var stepComponent = PhysicsStep.Default;
            if (buildPhysicsWorld.HasSingleton<PhysicsStep>())
            {
                stepComponent = buildPhysicsWorld.GetSingleton<PhysicsStep>();
            }

            // Extract original world and make copies
            List<PhysicsWorld> physicsWorlds = new List<PhysicsWorld>(k_NumWorlds);
            physicsWorlds.Add(buildPhysicsWorld.PhysicsWorld.Clone());
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
                SynchronizeCollisionWorld = true,
                TimeStep = Time.fixedDeltaTime
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

                    var simulationContext = new SimulationContext();
                    for (int step = 0; step < k_StopAfterStep; step++)
                    {
                        simulationContext.Reset(ref stepInput.World);
                        new StepJob
                        {
                            Input = stepInput,
                            SimulationContext = simulationContext
                        }.Schedule().Complete();
                    }

                    simulationContext.Dispose();
                }
                else
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
                EntitiesCleanup();
                yield return new WaitForFixedUpdate();
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
            EntitiesCleanup();
        }

        protected static void EntitiesCleanup()
        {
            var entityManager = DefaultWorld.EntityManager;
            var entities = entityManager.GetAllEntities();
            entityManager.DestroyEntity(entities);
            entities.Dispose();
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
    }
}
