using Unity.Mathematics;
using UnityEngine;

namespace TwoStickClassicExample
{
    public sealed class TwoStickBootstrap : MonoBehaviour
    {
        public static TwoStickExampleSettings Settings { get; private set; }

#pragma warning disable 649
        [SerializeField] private TwoStickExampleSettings settings;
#pragma warning restore 649

        public void Awake()
        {
            Settings = settings;
            var sceneSwitcher = GameObject.Find("SceneSwitcher");
            if (sceneSwitcher != null)
            {
                NewGame();
            }
        }

        public static void NewGame()
        {
            var player = Object.Instantiate(Settings.PlayerPrefab);
            player.Position = new float2(0, 0);
            player.Heading = new float2(0, 1);
        }
    }
}
