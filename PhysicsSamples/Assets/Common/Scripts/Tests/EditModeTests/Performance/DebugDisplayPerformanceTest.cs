using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.Scenes;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Physics.Tests.Performance
{
    public class DebugDisplayPerformanceTests
    {
        IEnumerator MeasurePerformance(int numWarmupFrames, int numMeasureFrames, string frameTimeName, Action<int> frameAction = null)
        {
            for (int i = 0; i < numWarmupFrames; ++i)
            {
                yield return null;
            }
            for (int i = 0; i < numMeasureFrames; ++i)
            {
                using var scope = Measure.Scope(frameTimeName);
                frameAction?.Invoke(i);
                yield return null;
            }
        }

        [UnityTest, Performance]
        public IEnumerator TestDebugDisplayPerformance()
        {
            const string scenePath = "Assets/Tests/Performance/DebugDisplayPerformanceTest.unity";
            if (!File.Exists(scenePath))
            {
                Assert.Inconclusive("The path to the Scene is not correct.");
            }

            EditorSceneManager.OpenScene(scenePath);
            var subScenes = SubScene.AllSubScenes;
            foreach (var subScene in subScenes)
            {
                // enable sub-scene for editing to ensure it is available immediately
                Scenes.Editor.SubSceneUtility.EditScene(subScene);
            }

            // locate camera game object
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            Assert.IsNotNull(camera);
            var cameraPos = camera.transform.localPosition;
            var cameraDeltaPos = new Vector3(0, 0.01f, 0);

            // measure during edit mode
            const int kMeasureFrames = 60;
            const int kWarmupFrames = 10;
            yield return MeasurePerformance(kWarmupFrames, kMeasureFrames, "EditMode Frame Time", (frameIndex)
                => camera.transform.localPosition = cameraPos + (frameIndex % 2) * cameraDeltaPos); // Note: we slightly move the camera every frame to trigger a re-render in EditMode

            // enter play mode and measure again during play mode
            yield return new EnterPlayMode();

            yield return MeasurePerformance(kWarmupFrames, kMeasureFrames, "PlayMode Frame Time");
        }
    }
}
