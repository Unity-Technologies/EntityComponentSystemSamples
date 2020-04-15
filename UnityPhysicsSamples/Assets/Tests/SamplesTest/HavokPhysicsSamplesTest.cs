using NUnit.Framework;
using System.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Physics.Samples.Test
{
#if HAVOK_PHYSICS_EXISTS
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    public class EnsureHavokPhysics : ComponentSystem
    {
        protected override void OnUpdate()
        {
            if (HasSingleton<PhysicsStep>())
            {
                var component = GetSingleton<PhysicsStep>();
                if (component.SimulationType != SimulationType.HavokPhysics)
                {
                    component.SimulationType = SimulationType.HavokPhysics;
                    SetSingleton(component);
                }
            }
            else
            {
                // Invalidate the physics world
                var buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BuildPhysicsWorld>();
                buildPhysicsWorld.PhysicsWorld.Reset(0, 0, 0);
            }
        }
    }

    [TestFixture]
    class HavokPhysicsSamplesTestMT : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(60000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(UnityPhysicsSamplesTest.GetScenes))] string scenePath)
        {
            // Don't create log messages about the number of trial days remaining
            PlayerPrefs.SetInt("Havok.Auth.SuppressDialogs", 1);

            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            var world = World.DefaultGameObjectInjectionWorld;

            // Ensure Havok
            var system = world.GetExistingSystem<EnsureHavokPhysics>();
            {
                Assert.IsNull(system, "The 'EnsureHavokPhysics' system should only be created by the 'HavokPhysicsSamplesTest.LoadScenes' function!");

                system = new EnsureHavokPhysics();
                world.AddSystem(system);
                world.GetExistingSystem<SimulationSystemGroup>().AddSystemToUpdateList(system);
            }

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            UnityPhysicsSamplesTest.EntitiesCleanup();
            yield return new WaitForFixedUpdate();

            world.GetExistingSystem<SimulationSystemGroup>().RemoveSystemFromUpdateList(system);
            world.DestroySystem(system);

            LogAssert.NoUnexpectedReceived();

            PlayerPrefs.DeleteKey("Havok.Auth.SuppressDialogs");
        }
    }

    [TestFixture]
    class HavokPhysicsSamplesTestST : UnityPhysicsSamplesTest
    {
        [UnityTest]
        [Timeout(60000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(UnityPhysicsSamplesTest.GetScenes))] string scenePath)
        {
            // Don't create log messages about the number of trial days remaining
            PlayerPrefs.SetInt("Havok.Auth.SuppressDialogs", 1);

            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            var world = World.DefaultGameObjectInjectionWorld;

            // Ensure Havok
            var system = world.GetExistingSystem<EnsureHavokPhysics>();
            {
                Assert.IsNull(system, "The 'EnsureHavokPhysics' system should only be created by the 'HavokPhysicsSamplesTest.LoadScenes' function!");

                system = new EnsureHavokPhysics();
                world.AddSystem(system);
                world.GetExistingSystem<SimulationSystemGroup>().AddSystemToUpdateList(system);
            }

            // Ensure ST simulation
            var stSystem = world.GetExistingSystem<EnsureSTSimulation>();
            {
                Assert.IsNull(stSystem, "The 'EnsureSTSimulation' system should only be created by the 'HavokPhysicsSamplesTest.LoadScenes' function!");

                stSystem = new EnsureSTSimulation();
                world.AddSystem(stSystem);
                world.GetExistingSystem<SimulationSystemGroup>().AddSystemToUpdateList(stSystem);
            }

            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            UnityPhysicsSamplesTest.EntitiesCleanup();
            yield return new WaitForFixedUpdate();

            world.GetExistingSystem<SimulationSystemGroup>().RemoveSystemFromUpdateList(system);
            world.DestroySystem(system);

            world.GetExistingSystem<SimulationSystemGroup>().RemoveSystemFromUpdateList(stSystem);
            world.DestroySystem(stSystem);

            LogAssert.NoUnexpectedReceived();

            PlayerPrefs.DeleteKey("Havok.Auth.SuppressDialogs");
        }
    }
#endif
}
