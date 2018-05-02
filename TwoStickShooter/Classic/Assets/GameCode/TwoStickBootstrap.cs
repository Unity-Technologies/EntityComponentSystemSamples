using Unity.Mathematics;
using UnityEngine;

namespace TwoStickClassicExample
{
    public sealed class TwoStickBootstrap : MonoBehaviour
    {
        public static TwoStickExampleSettings Settings { get; private set; }

        [SerializeField] private TwoStickExampleSettings settings;

        public void Awake()
        {
            Settings = settings;
        }

        public static void NewGame()
        {
            var player = Object.Instantiate(Settings.PlayerPrefab);
            player.Position = new float2(0, 0);
            player.Heading = new float2(0, 1);
        }
    }
}
