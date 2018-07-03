using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoStickHybridExample
{
    public sealed class TwoStickBootstrap
    {
        public static TwoStickExampleSettings Settings;
        
        public static void NewGame()
        {

            var player = Object.Instantiate(Settings.PlayerPrefab);
            player.GetComponent<Position2D>().Value = new float2(0, 0);
            player.GetComponent<Heading2D>().Value = new float2(0, 1);

        }
        
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeAfterSceneLoad()
        {
            var settingsGO = GameObject.Find("Settings");
            if (settingsGO == null)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                return;
            }
            
            InitializeWithScene();
        }

        private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            InitializeWithScene();
        }

        public static void InitializeWithScene()
        {
            var settingsGO = GameObject.Find("Settings");
            Settings = settingsGO?.GetComponent<TwoStickExampleSettings>();
            if (!Settings)
                return;

            EnemySpawnSystem.SetupComponentData();
            
            World.Active.GetOrCreateManager<UpdatePlayerHUD>().SetupGameObjects();
        }
    }
}
