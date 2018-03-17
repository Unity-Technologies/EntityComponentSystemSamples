using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
