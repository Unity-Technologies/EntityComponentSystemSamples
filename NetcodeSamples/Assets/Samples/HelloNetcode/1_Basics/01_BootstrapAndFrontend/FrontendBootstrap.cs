using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Samples.HelloNetcode
{
    public class FrontendBootstrap : MonoBehaviour
    {
        void Start()
        {
#if UNITY_SERVER
            string sceneName = "Asteroids";
#else
            string sceneName = "Frontend";
#endif

            // Commandline always overrides defaults if it exists
            string commandScene = CommandLineUtils.GetCommandLineValueFromKey("scene");
            if (!string.IsNullOrEmpty(commandScene))
            {
                var scene = SceneManager.GetSceneByName(commandScene);

                if (!scene.IsValid())
                    Debug.LogWarning($"Scene '{commandScene}' not found, using default '{sceneName}' instead");
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
