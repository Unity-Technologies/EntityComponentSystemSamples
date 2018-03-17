using Unity.Mathematics;
using UnityEngine;

namespace TwoStickClassicExample
{
    public sealed class TwoStickBootstrap
    {
        public static TwoStickExampleSettings Settings;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeWithScene()
        {
            var settingsGO = GameObject.Find("Settings");
            Settings = settingsGO?.GetComponent<TwoStickExampleSettings>();
        }

        public static void NewGame()
        {
            var player = Object.Instantiate(Settings.PlayerPrefab);
            player.Position = new float2(0, 0);
            player.Heading = new float2(0, 1);
        }
    }
}
