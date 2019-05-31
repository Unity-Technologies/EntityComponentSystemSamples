using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Physics.Samples.Test
{
#if HAVOK_PHYSICS_EXISTS
    [TestFixture]
    public class HavokPhysicsSamplesTest : UnityPhysicsSamplesTest
    {
        void SetSimulationType(SimulationType simulationType, Scene scene, LoadSceneMode mode)
        {
            var entityManager = World.Active.EntityManager;
            var entities = entityManager.GetAllEntities();
            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<PhysicsStep>(entity))
                {
                    PhysicsStep componentData = entityManager.GetComponentData<PhysicsStep>(entity);
                    componentData.SimulationType = simulationType;
                    entityManager.SetComponentData<PhysicsStep>(entity, componentData);
                    break;
                }
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public override IEnumerator LoadScenes([ValueSource(nameof(UnityPhysicsSamplesTest.GetScenes))] string scenePath)
        {
            // Log scene name in case Unity crashes and test results aren't written out.
            Debug.Log("Loading " + scenePath);
            LogAssert.Expect(LogType.Log, "Loading " + scenePath);

            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => SetSimulationType(SimulationType.HavokPhysics, scene, mode);
            SceneManager.LoadScene(scenePath);
            yield return new WaitForSeconds(1);
            UnityPhysicsSamplesTest.EntitiesCleanup();
            yield return new WaitForFixedUpdate();
            LogAssert.NoUnexpectedReceived();
        }
    }
#endif
}
