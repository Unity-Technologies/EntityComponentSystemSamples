using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine.Serialization;

namespace Samples.HelloNetcode
{
    public class Frontend : MonoBehaviour
    {
        const ushort k_NetworkPort = 7979;
        const string k_LastPlayedHelloNetcodeSceneKey = "Frontend.LastPlayedHelloNetcodeScene";
        const string k_LastPlayedSampleSceneKey = "Frontend.LastPlayedSampleScene";
        const string k_LastSamplePickerDropdownValueKey = "Frontend.LastSamplePickerDropdownValue";

        public InputField Address;
        public InputField Port;
        public Dropdown Sample;
        public Dropdown SamplePicker;
        public Button ClientServerButton;

        /// <summary>
        /// Stores the old name of the local world (create by initial bootstrap).
        /// It is reused later when the local world is created when coming back from game to the menu.
        /// </summary>
        internal static string OldFrontendWorldName = string.Empty;

        static string LastPlayedHelloNetcodeScene
        {
            get => PlayerPrefs.GetString(k_LastPlayedHelloNetcodeSceneKey, null);
            set => PlayerPrefs.SetString(k_LastPlayedHelloNetcodeSceneKey, value);
        }

        static string LastPlayedSampleScene
        {
            get => PlayerPrefs.GetString(k_LastPlayedSampleSceneKey, null);
            set => PlayerPrefs.SetString(k_LastPlayedSampleSceneKey, value);
        }

        static int LastSamplePickerDropdownValue
        {
            get => math.clamp(PlayerPrefs.GetInt(k_LastSamplePickerDropdownValueKey, 0), 0, 1);
            set => PlayerPrefs.SetInt(k_LastSamplePickerDropdownValueKey, value);
        }

        public void Start()
        {
            SamplePicker.value = LastSamplePickerDropdownValue;
            PopulateSampleDropdown(SamplePicker.value);
            ClientServerButton.gameObject.SetActive(ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer);
        }

        public void StartClientServer(string sceneName)
        {
            Debug.Log($"[StartClientServer] Called with '{sceneName}'.");
            if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
            {
                Debug.LogError($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
                return;
            }

            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

            SceneManager.LoadScene("FrontendHUD");

            //Destroy the local simulation world to avoid the game scene to be loaded into it
            //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
            //and other issues.
            DestroyLocalSimulationWorld();
            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = server;
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            var port = ParsePortOrDefault(Port.text);

            NetworkEndpoint ep = NetworkEndpoint.AnyIpv4.WithPort(port);
            {
                using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.RequireConnectionApproval = sceneName.Contains("ConnectionApproval", StringComparison.OrdinalIgnoreCase);
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(ep);
            }

            ep = NetworkEndpoint.LoopbackIpv4.WithPort(port);
            {
                using var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
            }
        }

        public void StartClientServer()
        {
            StartClientServer(GetAndSaveSceneSelection());
        }

        protected string GetAndSaveSceneSelection()
        {
            // Check whether Samples(0) or HelloNetcodeSamples(1) are selected in the sample picker
            LastSamplePickerDropdownValue = SamplePicker.value;
            string sceneName = Sample.options[Sample.value].text;
            LastPlayedSampleScene = sceneName;
            PlayerPrefs.Save();
            return sceneName;
        }

        public void ConnectToServer()
        {
            Debug.Log($"[ConnectToServer] Called on '{Address.text}:{Port.text}'.");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            SceneManager.LoadScene("FrontendHUD");
            DestroyLocalSimulationWorld();

            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = client;
            var sceneName = GetAndSaveSceneSelection();
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            var ep = NetworkEndpoint.Parse(Address.text, ParsePortOrDefault(Port.text));
            {
                using var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
            }
        }

        // Populate the scene dropdown depending on if samples/hellonetcode selection is picked in the first
        // dropdown. Always skip frontend scene since that's the one which is showing this menu and makes no sense to
        // load (as well as HUD scenes since they are additively loaded on top of sample scenes)
        public void PopulateSampleDropdown(int value)
        {
            var scenes = SceneManager.sceneCountInBuildSettings;
            Sample.ClearOptions();
            if (value == 0)
            {
                var lastPlayed = LastPlayedSampleScene;
                for (var i = 0; i < scenes; ++i)
                {
                    var sceneName = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                    var isHelloNetcodeScene = SceneUtility.GetScenePathByBuildIndex(i).Contains("HelloNetcode");
                    if (!sceneName.StartsWith("Frontend") && !sceneName.EndsWith("HUD") && !isHelloNetcodeScene)
                    {
                        Sample.options.Add(new Dropdown.OptionData { text = sceneName });
                        if (string.Equals(sceneName, lastPlayed, StringComparison.OrdinalIgnoreCase))
                            Sample.value = Sample.options.Count;
                    }
                }
            }
            else if (value == 1)
            {
                string lastPlayed = LastPlayedHelloNetcodeScene;
                for (var i = 0; i < scenes; ++i)
                {
                    var sceneName = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                    var isHelloNetcodeScene = SceneUtility.GetScenePathByBuildIndex(i).Contains("HelloNetcode");
                    if (!sceneName.StartsWith("Frontend") && !sceneName.EndsWith("HUD") && isHelloNetcodeScene)
                    {
                        Sample.options.Add(new Dropdown.OptionData { text = sceneName });
                        if (string.Equals(sceneName, lastPlayed, StringComparison.OrdinalIgnoreCase))
                            Sample.value = Sample.options.Count;
                    }
                }
            }
            else
            {
                Debug.LogError("Invalid dropdown value");
            }

            Sample.RefreshShownValue();
        }

        protected void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    OldFrontendWorldName = world.Name;
                    world.Dispose();
                    break;
                }
            }
        }

        // Tries to parse a port, returns true if successful, otherwise false
        // The port will be set to whatever is parsed, otherwise the default port of k_NetworkPort
        private UInt16 ParsePortOrDefault(string s)
        {
            if (!UInt16.TryParse(s, out var port))
            {
                Debug.LogWarning($"Unable to parse port, using default port {k_NetworkPort}");
                return k_NetworkPort;
            }

            return port;
        }
    }
}
