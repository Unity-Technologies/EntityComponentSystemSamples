using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities.Conversion;
using Unity.PerformanceTesting;
using Unity.Scenes;
using Unity.Scenes.Editor;
using Unity.Scenes.Editor.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Entities.Baking
{
    [TestFixture,Serializable]
    class PerformanceTests
    {
        [SerializeField] private Scene _scene;
        [SerializeField] private SubScene _subScene;
        [SerializeField] private string[] _objectNameArray;
        [SerializeField] private string[] _sampleGroupStrs;
        [SerializeField] private SampleGroup[] _sampleGroups;
        [SerializeField] private TestLiveConversionSettings m_Settings;

        protected const int MaxIterations = 5;

        private const string ScenePath = "Assets/Advanced/MegaCityBuilding/SingleSection.unity";
        private const string RootGameObjectName = "Backdrop_Building_C (3)";
        private const string IntermediateGameObjectName = "Geom_Aircondition_Unit_B_LOD00";
        private const string LeafGameObjectName = "CombinedLowLOD";

        internal enum MegacityObjectInHierarchy
        {
            Root = 0,
            Intermediate,
            Leaf,
            ExternalToBuilding
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            _subScene = Object.FindObjectOfType<SubScene>();
            Assert.NotNull(_subScene);

            _objectNameArray = new[] { RootGameObjectName, IntermediateGameObjectName, LeafGameObjectName };

            List<string> sampleGroupStrList = new List<string>();
            sampleGroupStrList.AddRange(BakingUtility.CollectImportantProfilerMarkerStrings());
            sampleGroupStrList.AddRange(IncrementalBakingContext.CollectImportantProfilerMarkerStrings());
            sampleGroupStrList.AddRange(BakeDependencies.CollectImportantProfilerMarkerStrings());
            sampleGroupStrList.AddRange(EntityDiffer.CollectImportantProfilerMarkerStrings());
            sampleGroupStrList.AddRange(EntityPatcher.CollectImportantProfilerMarkerStrings());

            _sampleGroupStrs = sampleGroupStrList.ToArray();
            _sampleGroups = new SampleGroup[sampleGroupStrList.Count];
        }

        [SetUp]
        public void SetUp()
        {
            m_Settings.Setup(true);

            LiveConversionSettings.TreatIncrementalConversionFailureAsError = false;
            LiveConversionSettings.EnableInternalDebugValidation = false;
            LiveConversionSettings.Mode = LiveConversionSettings.ConversionMode.IncrementalConversion;

            for (int index = 0; index < _sampleGroups.Length; ++index)
            {
                _sampleGroups[index] = new SampleGroup(_sampleGroupStrs[index], SampleUnit.Millisecond);
            }
        }

        [TearDown]
        public void TearDown()
        {
            CloseScene();
            m_Settings.TearDown();
        }

        internal void UnpackPrefabInstance()
        {
            var root = GetBuildingRoot();
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        private GameObject GetBuildingRoot()
        {
            var root = GameObject.Find(RootGameObjectName);
            Assert.NotNull(root);
            return root;
        }

        private string GetGameObjectName(MegacityObjectInHierarchy inHierarchy)
        {
            if (inHierarchy != MegacityObjectInHierarchy.ExternalToBuilding)
                return _objectNameArray[(int)inHierarchy];
            return null;
        }

        private GameObject GetOrCreateGameObject(MegacityObjectInHierarchy inHierarchy, out bool newObject)
        {
            if (inHierarchy != MegacityObjectInHierarchy.ExternalToBuilding)
            {
                newObject = false;
                var name = GetGameObjectName(inHierarchy);
                return GetGameObject(name);
            }
            newObject = true;
            return InstantiateCube(null);
        }

        private GameObject GetGameObject(string name)
        {
            var root = GameObject.Find(name);
            Assert.NotNull(root);
            return root;
        }

        private void UpdateWorld()
        {
            World.DefaultGameObjectInjectionWorld.Update();
        }

        private void EditAndBakeScene()
        {
            SubSceneUtility.EditScene(_subScene);
            UpdateWorld();
        }

        private void CloseScene()
        {
            if (_subScene.IsLoaded)
            {
                SubSceneInspectorUtility.CloseSceneWithoutSaving(_subScene);
            }
        }

        private GameObject InstantiateCube(GameObject parent)
        {
            SceneManager.SetActiveScene(_subScene.EditingScene);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (parent != null)
            {
                cube.transform.SetParent(parent.transform);
            }
            Undo.RegisterCreatedObjectUndo(cube, "Cube");
            Undo.FlushUndoRecordObjects();
            return cube;
        }

        public class AuthoringComponentTest1 : MonoBehaviour { public int Field; }

        struct ComponentTest1 : IComponentData
        {
            public int Field;
        }

        class ComponentTest1Baker : Baker<AuthoringComponentTest1>
        {
            public override void Bake(AuthoringComponentTest1 component)
            {
                AddComponent(ComponentType.ReadWrite<ComponentTest1>());
            }
        }

        [UnityTest, Performance]
        public IEnumerator FullBaking()
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    EditAndBakeScene();
                }

                CloseScene();
            }

            yield break;
        }

        [UnityTest, Performance]
        public IEnumerator MoveGameObject([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Move the gameobject
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }

                Undo.RecordObject(go.transform, "Change value");
                go.transform.position += Vector3.one;
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                CloseScene();
            }
        }

        [UnityTest, Performance]
        public IEnumerator EnableGameObject([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Disable gameobject first
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }

                Undo.RecordObject(go, "Change value");
                go.SetActive(false);
                Undo.FlushUndoRecordObjects();

                yield return null;
                UpdateWorld();

                // Reenable gameobject
                Undo.RecordObject(go, "Change value");
                go.SetActive(true);
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                CloseScene();
            }
        }

        [UnityTest, Performance]
        public IEnumerator DisableGameObject([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Disable gameobject
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }

                Undo.RecordObject(go, "Change value");
                go.SetActive(false);
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                CloseScene();
            }
        }

        [UnityTest, Performance]
        public IEnumerator CreateGameObject([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Break the prefab connection
                UnpackPrefabInstance();

                // Create gameobject
                var parent = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                InstantiateCube(parent);

                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                CloseScene();
            }
            yield break;
        }

        [UnityTest, Performance]
        public IEnumerator DeleteGameObject([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Break the prefab connection
                UnpackPrefabInstance();

                // Destroy gameobject
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }
                Undo.DestroyObjectImmediate(go);
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                CloseScene();
            }
        }

        [UnityTest, Performance]
        public IEnumerator FlipWindingGameObject([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Flip Winding for the gameobject
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }

                Undo.RecordObject(go.transform, "Change value");
                var scale = go.transform.localScale;
                scale.y = -scale.y;
                go.transform.localScale = scale;
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                CloseScene();
            }
        }

        [UnityTest, Performance]
        public IEnumerator StaticGameObject([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Remove static from the root if needed
                var root = GetBuildingRoot();
                var staticComponentRoot = root.GetComponent<StaticOptimizeEntity>();
                if (staticComponentRoot != null)
                {
                    Undo.DestroyObjectImmediate(staticComponentRoot);
                    Undo.IncrementCurrentGroup();

                    yield return null;
                    UpdateWorld();
                }

                // Add static to the gameobject
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }

                Undo.RecordObject(go, "Added Component");
                Undo.AddComponent<StaticOptimizeEntity>(go);
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                CloseScene();
            }
        }

        [UnityTest, Performance]
        public IEnumerator AddComponent([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Add static to the gameobject
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }

                Undo.AddComponent<AuthoringComponentTest1>(go);
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                // Sanity check that the ECS component it's in there
                var componentQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<ComponentTest1>());
                Assert.AreEqual(1, componentQuery.CalculateEntityCount(), "Expected a game object to be converted");

                CloseScene();
            }
        }

        [UnityTest, Performance]
        public IEnumerator RemoveComponent([Values] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Add static to the gameobject first
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }

                Undo.AddComponent<AuthoringComponentTest1>(go);
                Undo.FlushUndoRecordObjects();

                yield return null;
                UpdateWorld();

                // Sanity check that the ECS component it's in there
                var componentQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<ComponentTest1>());
                Assert.AreEqual(1, componentQuery.CalculateEntityCount(), "Expected a game object to be converted");

                // Now remove the component
                var component = go.GetComponent<AuthoringComponentTest1>();
                Undo.DestroyObjectImmediate(component);
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                Assert.AreEqual(0, componentQuery.CalculateEntityCount(), "Expected a game object to be converted");

                CloseScene();
            }
        }

        [UnityTest, Performance]
        public IEnumerator ChangeComponent([Values(MegacityObjectInHierarchy.Leaf)] MegacityObjectInHierarchy gameObjectInHierarchy)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                // Add static to the gameobject
                var go = GetOrCreateGameObject(gameObjectInHierarchy, out bool newObject);
                if (newObject)
                {
                    yield return null;
                    UpdateWorld();
                }

                var renderer = go.GetComponent<MeshRenderer>();
                Undo.RecordObject(renderer, "Modified renderer");
                renderer.receiveShadows = !renderer.receiveShadows;
                Undo.FlushUndoRecordObjects();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                CloseScene();
            }
        }


         // This test has some issues, it will be restored as part of a different ticket (DOTS-5646)
         // The baking happens in 2 different frames (triggered first by assets and then structural changes) and some investigation is needed to determine if that behaviour is correct
         // There are also some warnings from the LODGroups that are triggered by this test. This needs to be investigated too.
         // Modifying the global scale seems to be making the meta file appeared as modified in git.
        [UnityTest, Performance, Ignore("Test behaviour needs reviewing")]
        public IEnumerator ChangeModelAsset()
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                EditAndBakeScene();

                var modelImporter = AssetImporter.GetAtPath("Assets/Advanced/MegaCityBuilding/Models/Environment/Aircondition_Units_A/Aircondition_Unit_A.FBX") as ModelImporter;
                Assert.NotNull(modelImporter);

                var originalScale = modelImporter.globalScale;
                modelImporter.globalScale += 10.0f;
                modelImporter.SaveAndReimport();

                // Measure Baking Markers
                yield return null;
                using (Measure.ProfilerMarkers(_sampleGroups))
                {
                    UpdateWorld();
                }

                // Undo the change
                modelImporter.globalScale = originalScale;
                modelImporter.SaveAndReimport();

                CloseScene();
            }
        }
    }
}

