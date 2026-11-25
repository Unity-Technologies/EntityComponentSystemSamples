using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameObjectSample
{
    public class NetworkManager : MonoBehaviour
    {
        const string k_Address = "127.0.0.1";
        const int k_Port = 7777;
        float m_SpawnPosition;

        public bool AutomaticInGame {get; set;}
        public bool SingleWorldHost {get; set;}

        public Button SceneOne;
        public Button SceneTwo;
        public Button SubSceneOne;
        public Button SubSceneTwo;
        public Button SceneOneSpawn;
        public Button SceneTwoSpawn;
        public Button SubSceneOneSpawn;
        public Button SubSceneTwoSpawn;
        public Button DisconnectButton;
        public Button Host;
        public Button Connect;

        void Start()
        {
            SingleWorldHost = AutomaticInGame = true;
            Reset();
            SceneManager.LoadScene("GhostPrefabsDisplayHUD", LoadSceneMode.Additive);
        }

        void Reset()
        {
            Netcode.OnConnectionEvent += NetcodeOnOnConnectionEvent;
            m_SpawnPosition = -3.0f;
        }

        public void StartHost()
        {
            if (SingleWorldHost)
            {
                Netcode.StartAsHost(NetworkEndpoint.AnyIpv4.WithPort(k_Port));
            }
            else
            {
                Netcode.Listen(NetworkEndpoint.AnyIpv4.WithPort(k_Port));
                Netcode.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(k_Port));
            }

            DestroyLocalSimulationWorld();

            SetStartedState();

            // Check if any scenes with ghosts have already been loaded and then enable the spawn buttons.
            // This is not needed for the subscenes as they require a world (which we just created) to load into.
            EnableButtonsForLoadedScene("Scene1");
            EnableButtonsForLoadedScene("Scene2");
        }

        void EnableButtonsForLoadedScene(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
                EnableDisableSpawnButtons(sceneName);
        }

        void NetcodeOnOnConnectionEvent(Connection connection, NetcodeConnectionEvent connectionEvent)
        {
            if (AutomaticInGame && connectionEvent.State == ConnectionState.State.Connected)
            {
                Debug.Log($"Automatic InGame for connection {connection.NetworkId}");
                connection.World.EntityManager.AddComponent<NetworkStreamInGame>(connection.ConnectionEntity);
            }
        }

        public void ConnectToServer()
        {
            Netcode.Connect(NetworkEndpoint.Parse(k_Address, k_Port));
            SetStartedState();
        }

        void SetStartedState()
        {
            SubSceneOne.interactable = true;
            SubSceneTwo.interactable = true;
            DisconnectButton.gameObject.SetActive(true);
            Host.gameObject.SetActive(false);
            Connect.gameObject.SetActive(false);
        }

        /// <summary>
        /// Disconnecting resets everything into the initial start state
        /// </summary>
        public void Disconnect()
        {
            Netcode.Shutdown(reset: true);
            Reset();

            UnloadScene("Scene1", SceneOne.GetComponentInChildren<Text>());
            UnloadScene("Scene2", SceneTwo.GetComponentInChildren<Text>());
            UnloadScene("SubScene1", SubSceneOne.GetComponentInChildren<Text>());
            UnloadScene("SubScene2", SubSceneTwo.GetComponentInChildren<Text>());

            SubSceneOne.interactable = false;
            SubSceneTwo.interactable = false;

            SceneOneSpawn.interactable = false;
            SceneTwoSpawn.interactable = false;
            SubSceneOneSpawn.interactable = false;
            SubSceneTwoSpawn.interactable = false;

            var prefabDisplay = FindAnyObjectByType<Samples.HelloNetcode.GhostPrefabListDisplay>();
            if (prefabDisplay != null)
                prefabDisplay.Clear();

            DisconnectButton.gameObject.SetActive(false);
            Host.gameObject.SetActive(true);
            Connect.gameObject.SetActive(true);
        }

        public void ToggleInGame()
        {
            foreach (var world in World.All)
            {
                if (!ClientServerWorldExtensions.IsClient(world) && !ClientServerWorldExtensions.IsServer(world))
                    continue;
                using var connectionQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamConnection>());
                var connections = connectionQuery.ToEntityArray(Allocator.Temp);
                foreach (var connection in connections)
                {
                    if (!world.EntityManager.HasComponent<NetworkStreamInGame>(connection))
                    {
                        Debug.Log($"Add connection {connection} in game");
                        world.EntityManager.AddComponent<NetworkStreamInGame>(connection);
                    }
                    else
                    {
                        Debug.Log($"Remove connection {connection} in game");
                        world.EntityManager.RemoveComponent<NetworkStreamInGame>(connection);
                    }
                }
            }
        }

        public void SpawnCube1()
        {
            LookupAndSpawn("Cube1");
        }

        public void SpawnCube2()
        {
            LookupAndSpawn("Cube2");
        }

        void LookupAndSpawn(string prefabName)
        {
            var registries = FindObjectsByType<PrefabRegistry>();
            var foundPrefab = false;
            foreach (var registry in registries)
            {
                foreach (var prefab in registry.Prefabs)
                {
                    // Must already have a world to spawn the ghost, or the game object <-> entity link setup will fail
                    if (prefab.name == prefabName)
                    {
                        var spawnPos = new Vector3(m_SpawnPosition++, 0f, 0f);
                        Debug.Log($"Spawn via PrefabRegistry {prefab.name} at {spawnPos}");
                        var spawned = (GameObject)Instantiate(prefab, registry.gameObject.scene);
                        spawned.transform.position = spawnPos;
                        var ghostAdapter = spawned.GetComponent<GhostObject>();
                        var entityManager = spawned.NetcodeWorld().EntityManager;
                        var localTransform = entityManager.GetComponentData<LocalTransform>(ghostAdapter.Entity);
                        localTransform.Position = spawnPos;
                        entityManager.SetComponentData(ghostAdapter.Entity, localTransform);
                        foundPrefab = true;
                        break;  // There could be multiple instances of the spawner, so use the first one found
                    }
                }
            }
            if (!foundPrefab)
                Debug.LogError($"Failed to lookup {prefabName} in prefab registries");
        }

        public void SpawnCubeWithSubScene1Tag()
        {
            SpawnEntityGhostPrefab(ComponentType.ReadOnly<CubeFromSubScene1Tag>());
        }

        public void SpawnCubeWithSubScene2Tag()
        {
            SpawnEntityGhostPrefab(ComponentType.ReadOnly<CubeFromSubScene2Tag>());
        }

        public void SpawnEntityGhostPrefab(ComponentType componentType)
        {
            var serverWorld = ClientServerBootstrap.ServerWorld;
            if (serverWorld == null)
                return;
            var builder = new EntityQueryBuilder(Allocator.Temp);
            FixedList32Bytes<ComponentType> componentTypes = default;
            componentTypes.Add(componentType);
            builder.WithAll(ref componentTypes);
            builder.WithAll<Prefab>();
            builder.WithOptions(EntityQueryOptions.IncludePrefab);
            using var prefabQuery = serverWorld.EntityManager.CreateEntityQuery(builder);
            var prefabs =  prefabQuery.ToEntityArray(Allocator.Temp);
            if (prefabs.Length > 0)
            {
                var ghostPrefab = prefabs[0];
                var spawnPos = new float3(m_SpawnPosition++, 0, 0);
                Debug.Log($"Spawning {ghostPrefab} at {spawnPos}");
                var spawned = serverWorld.EntityManager.Instantiate(ghostPrefab);
                var localTransform = serverWorld.EntityManager.GetComponentData<LocalTransform>(spawned);
                localTransform.Position = spawnPos;
                serverWorld.EntityManager.SetComponentData(spawned, localTransform);
            }
            else
            {
                Debug.LogError($"Failed to find {componentType} prefab");
            }
        }

        public void LoadOrUnloadLevel(int level)
        {
            switch (level)
            {
                case 1: LoadOrUnloadScene("Scene1", SceneOne.GetComponentInChildren<Text>()); break;
                case 2: LoadOrUnloadScene("Scene2", SceneTwo.GetComponentInChildren<Text>()); break;
                case 3: LoadOrUnloadScene("SubScene1", SubSceneOne.GetComponentInChildren<Text>()); break;
                case 4: LoadOrUnloadScene("SubScene2", SubSceneTwo.GetComponentInChildren<Text>()); break;
            }
        }

        void UnloadScene(string sceneName, Text buttonLabel)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
                return;

            // When using SceneManager.UnloadSceneAsync with subscenes it might cause the client world to unload
            // the subscene inside and cleanup the ghosts in that subscene before the server has had the chance to
            // despawn them so we'll explicitly only unload on the server world first and wait a second before
            // unloading the scene which affects both worlds.
            var serverWorld = ClientServerBootstrap.ServerWorld;
            if (sceneName.StartsWith("SubScene") && serverWorld != null && serverWorld.IsCreated)
            {
                foreach (var go in scene.GetRootGameObjects())
                {
                    var subscene = go.GetComponentInChildren<SubScene>();
                    if (subscene != null)
                    {
                        Debug.Log($"Unloading subscene {sceneName} on server");
                        SceneSystem.UnloadScene(serverWorld.Unmanaged, subscene.SceneGUID);
                        StartCoroutine(WaitUntilSafeToUnloadOnLocalClient(sceneName));
                        break;
                    }
                }
            }
            else
            {
                Debug.Log($"Unloading {sceneName}");
                SceneManager.UnloadSceneAsync(scene);
            }
            EnableDisableSpawnButtons(sceneName);
            buttonLabel.text = $"Load{sceneName}";
        }

        void LoadOrUnloadScene(string sceneName, Text buttonLabel)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                UnloadScene(sceneName, buttonLabel);
            }
            else
            {
                Debug.Log($"Loading {sceneName}");
                SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                EnableDisableSpawnButtons(sceneName);
                buttonLabel.text = $"Unload{sceneName}";
            }
        }

        void EnableDisableSpawnButtons(string sceneName)
        {
            if (!ClientServerBootstrap.HasServerWorld)
                return;
            switch (sceneName)
            {
                case "Scene1": SceneOneSpawn.interactable = !SceneOneSpawn.interactable; break;
                case "Scene2": SceneTwoSpawn.interactable = !SceneTwoSpawn.interactable; break;
                case "SubScene1": SubSceneOneSpawn.interactable = !SubSceneOneSpawn.interactable; break;
                case "SubScene2": SubSceneTwoSpawn.interactable = !SubSceneTwoSpawn.interactable; break;
            }
        }

        /// <summary>
        /// When unloading with server+client worlds both present we need to wait a bit until the
        /// ghosts spawned from that scene have been despawned safely on the local client, assuming
        /// it's in game while this is done. First the subscene within the given sceneName should be
        /// unloaded only in the server world.
        /// </summary>
        IEnumerator WaitUntilSafeToUnloadOnLocalClient(string sceneName)
        {
            yield return new WaitForSeconds(1F);
            SceneManager.UnloadSceneAsync(sceneName);
        }

        protected void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }
    }
}
