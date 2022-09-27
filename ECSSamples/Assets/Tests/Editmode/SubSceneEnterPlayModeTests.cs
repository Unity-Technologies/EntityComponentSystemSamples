using NUnit.Framework;
using System.Collections;
using Unity.Scenes;
using Unity.Scenes.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EnterPlayModeTests
{
    public class SubSceneEnterPlayModeTests : BaseEnterPlayModeTest
    {
        const string k_SubscenePath = "Assets/HelloCube/1. MainThread/HelloCube_MainThread.unity";

        [UnityTest]
        [Ignore("DOTS-3424")]
        public IEnumerator SubScene_OpenedForEdit_RemainsOpenInPlayMode([Values(EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload)] EnterPlayModeOptions enterPlayModeOptions)
        {
            // Set editor settings
            EditorSettings.enterPlayModeOptionsEnabled = enterPlayModeOptions != EnterPlayModeOptions.None;
            EditorSettings.enterPlayModeOptions = enterPlayModeOptions;
            var expectingDomainReload = !enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload);

            DisableSetupAndTearDown();

            EditorSceneManager.OpenScene(k_SubscenePath, OpenSceneMode.Single);
            var subScene = Object.FindObjectOfType<SubScene>();
            Assert.NotNull(subScene);
            SubSceneUtility.EditScene(subScene);
            Assert.IsTrue(subScene.IsLoaded);
            Assert.AreEqual(2, SceneManager.sceneCount);

            // Enter play mode
            yield return new EnterPlayMode(expectingDomainReload);

            // Validate that subscene is still loaded
            subScene = Object.FindObjectOfType<SubScene>();
            Assert.NotNull(subScene);
            Assert.IsTrue(subScene.IsLoaded);
            Assert.AreEqual(2, SceneManager.sceneCount);

            yield return new ExitPlayMode();

            EnableSetupAndTearDown();
        }

        [UnityTest]
        // Disabled on Linux because it hangs during asset import - likely related to DOTS-3424 and running on the latest Ubuntu Bokken VM
        [UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor})]
        public IEnumerator SubScene_NotOpenedForEdit_RemainsClosedInPlayMode([Values(EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload)] EnterPlayModeOptions enterPlayModeOptions)
        {
            // Set editor settings
            EditorSettings.enterPlayModeOptionsEnabled = enterPlayModeOptions != EnterPlayModeOptions.None;
            EditorSettings.enterPlayModeOptions = enterPlayModeOptions;
            var expectingDomainReload = !enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload);

            DisableSetupAndTearDown();

            EditorSceneManager.OpenScene(k_SubscenePath, OpenSceneMode.Single);
            var subScene = Object.FindObjectOfType<SubScene>();
            Assert.NotNull(subScene);
            Assert.IsFalse(subScene.IsLoaded);
            Assert.AreEqual(1, SceneManager.sceneCount);

            // Enter play mode
            yield return new EnterPlayMode(expectingDomainReload);

            // Validate that subscene is still loaded
            subScene = Object.FindObjectOfType<SubScene>();
            Assert.NotNull(subScene);
            Assert.IsFalse(subScene.IsLoaded);
            Assert.AreEqual(1, SceneManager.sceneCount);

            yield return new ExitPlayMode();

            EnableSetupAndTearDown();
        }

        [UnityTest]
        // Disabled on Linux because it hangs during asset import - likely related to DOTS-3424 and running on the latest Ubuntu Bokken VM
        [UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor})]
        public IEnumerator SubScene_OpenedForEdit_UnloadsSceneOnDestroyImmediate()
        {
            EditorSceneManager.OpenScene(k_SubscenePath, OpenSceneMode.Single);
            var subScene = Object.FindObjectOfType<SubScene>();
            Assert.NotNull(subScene);
            SubSceneUtility.EditScene(subScene);
            Assert.IsTrue(subScene.IsLoaded);
            Assert.AreEqual(2, SceneManager.sceneCount);

            Object.DestroyImmediate(subScene.gameObject);

            yield return null;

            Assert.AreEqual(1, SceneManager.sceneCount);
        }
    }
}
