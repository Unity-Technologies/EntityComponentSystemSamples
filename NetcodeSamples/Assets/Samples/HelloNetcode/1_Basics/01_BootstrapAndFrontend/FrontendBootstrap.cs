using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Samples.HelloNetcode
{
    public class FrontendBootstrap : MonoBehaviour
    {
        void Start()
        {
#if UNITY_SERVER
            string defaultSceneName = "Asteroids";
#else
            string defaultSceneName = "Frontend";
#endif
            // Commandline always overrides defaults if it exists
            string commandScene = CommandLineUtils.GetCommandLineValueFromKey("scene");
            if (string.IsNullOrWhiteSpace(commandScene))
            {
                SceneManager.LoadScene(defaultSceneName);
                return;
            }
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var scene = Path.GetFileNameWithoutExtension(scenePath);
                if (commandScene == scene)
                {

                    SceneManager.LoadScene(commandScene);
                    return;
                }
            }
            Debug.LogError($"${commandScene} not found. Scenes present in the build\n: {string.Join(',', GetAllScenesInBuild())}");
            Application.Quit(-1);
        }

        private IEnumerable<string> GetAllScenesInBuild()
        {

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                yield return Path.GetFileNameWithoutExtension(scenePath);
            }
        }
    }
}
