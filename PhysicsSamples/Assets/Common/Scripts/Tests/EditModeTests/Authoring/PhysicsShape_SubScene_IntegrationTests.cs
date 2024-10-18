using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Physics.Tests.Authoring
{
    // Physics shape conversion tests for the sub-scene workflow
    class PhysicsShape_SubScene_IntegrationTests
        : ConversionSystem_SubScene_IntegrationTestsFixture
    {
        // Creates a sub-scene, populates it and loads it.
        // Then, performs validation action, enters play mode and again performs validation action.
        IEnumerator BaseColliderSubSceneTest(Action createSubSceneObjects, Action validation)
        {
            // create a sub-scene, populate it and load it.
            Assert.IsNull(SubSceneManaged);
            Assert.AreEqual(Entity.Null, SubSceneEntity);

            // create sub-scene
            CreateAndLoadSubScene(createSubSceneObjects);
            Assert.IsNotNull(SubSceneManaged);

            // wait until sub-scene is loaded by skipping frames
            while (!SceneSystem.IsSceneLoaded(World.DefaultGameObjectInjectionWorld.Unmanaged, SubSceneEntity))
            {
                yield return null;
            }

            // enable sub-scene for editing
            Scenes.Editor.SubSceneUtility.EditScene(SubSceneManaged);

            // Phase 1:
            // make sure we are in edit mode and validate
            Assume.That(Application.isPlaying, Is.False);

            // call validation function
            validation();

            // Phase 2:
            // enter play mode and validate
            yield return new EnterPlayMode();

            // make sure we are in play mode before validating
            while (!Application.isPlaying)
            {
                yield return null;
            }

            // call validation function
            validation();
        }

        // Tests that collider blobs in physics colliders are shared if they are identical
        [UnityTest]
        public IEnumerator TestSharedColliderBlobs()
        {
            PhysicsShapeAuthoring collider1, collider2;
            Action creation = () =>
            {
                collider1 = new GameObject(TestNameWithoutSpecialCharacters).AddComponent<PhysicsShapeAuthoring>();
                collider2 = new GameObject(TestNameWithoutSpecialCharacters).AddComponent<PhysicsShapeAuthoring>();

                // we don't want actual collisions to occur in this test
                collider1.CollisionResponse = CollisionResponsePolicy.RaiseTriggerEvents;
                collider2.CollisionResponse = CollisionResponsePolicy.RaiseTriggerEvents;

                // use identical colliders
                collider1.SetBox(default);
                collider2.SetBox(default);

                // make sure that the identical colliders can share a single collider blob by disabling the "force unique" setting
                collider1.ForceUnique = false;
                collider2.ForceUnique = false;
            };

            Action validation = () =>
            {
                unsafe
                {
                    using (var group = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PhysicsCollider>()))
                    {
                        using var colliderComponents = group.ToComponentDataArray<PhysicsCollider>(Allocator.Temp);
                        Assume.That(colliderComponents, Has.Length.EqualTo(2));
                        var colliderComponent1 = colliderComponents[0];
                        var colliderComponent2 = colliderComponents[1];
                        // make sure that the two collider blobs are shared and their pointers are thus identical
                        Assume.That((IntPtr)colliderComponent1.ColliderPtr, Is.EqualTo((IntPtr)colliderComponent2.ColliderPtr));

                        // make sure that the colliders indicate that they are not unique.
                        foreach (var collider in colliderComponents)
                        {
                            Assume.That(collider.IsUnique, Is.False);
                        }
                    }
                }
            };

            return BaseColliderSubSceneTest(creation, validation);
        }

        // Tests that collider blobs in physics colliders are unique despite being identical if they are forced to be unique
        [UnityTest]
        public IEnumerator TestUniqueColliderBlobs()
        {
            PhysicsShapeAuthoring collider1, collider2;
            Action creation = () =>
            {
                collider1 = new GameObject(TestNameWithoutSpecialCharacters).AddComponent<PhysicsShapeAuthoring>();
                collider2 = new GameObject(TestNameWithoutSpecialCharacters).AddComponent<PhysicsShapeAuthoring>();

                // we don't want actual collisions to occur in this test
                collider1.CollisionResponse = CollisionResponsePolicy.RaiseTriggerEvents;
                collider2.CollisionResponse = CollisionResponsePolicy.RaiseTriggerEvents;

                // use identical colliders
                collider1.SetBox(default);
                collider2.SetBox(default);

                // force the collider blobs to be unique in both PhysicsCollider components
                collider1.ForceUnique = true;
                collider2.ForceUnique = true;
            };

            Action validation = () =>
            {
                unsafe
                {
                    using (var group = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PhysicsCollider>()))
                    {
                        using var colliderComponents = group.ToComponentDataArray<PhysicsCollider>(Allocator.Temp);
                        Assume.That(colliderComponents, Has.Length.EqualTo(2));
                        var colliderComponent1 = colliderComponents[0];
                        var colliderComponent2 = colliderComponents[1];
                        // make sure that the two collider blobs are not identical
                        Assume.That((IntPtr)colliderComponent1.ColliderPtr, Is.Not.EqualTo((IntPtr)colliderComponent2.ColliderPtr));

                        // make sure that the colliders indicate that they are unique.
                        foreach (var collider in colliderComponents)
                        {
                            Assume.That(collider.IsUnique, Is.True);
                        }
                    }
                }
            };

            return BaseColliderSubSceneTest(creation, validation);
        }
    }
}
