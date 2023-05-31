#if !UNITY_DOTSRUNTIME && (!UNITY_SERVER || UNITY_EDITOR)
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UI;
using Hash128 = Unity.Entities.Hash128;

public class LevelManager : MonoBehaviour
{
    // TODO: subscene names are printed via entity names, which works in debug builds, we could
    //       save the names during conversion to get a more permanent place for them
    // TODO: is using ghost sync on/off instead of ingame on/off, should probably be changed...

    public Text ServerStatus;
    public Text ClientStatus;
    public Button ClientSyncButton;
    public Button ServerSyncButton;

    private World m_ServerWorld;
    private World ServerWorld
    {
        get
        {
            if (m_ServerWorld == null)
            {
                foreach (var world in World.All)
                {
                    if (world.IsServer())
                    {
                        m_ServerWorld = world;
                        break;
                    }
                }
            }
            return m_ServerWorld;
        }
    }

    public void SendLoadNextLevelCommand()
    {
        World clientOne = null;
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                clientOne = world;
                break;
            }
        }
        if (clientOne == null)
        {
            UnityEngine.Debug.LogError("No client world found");
            return;
        }

        var rpcCmd = clientOne.EntityManager.CreateEntity();
        clientOne.EntityManager.AddComponentData(rpcCmd, new LoadNextLevelCommand());
        clientOne.EntityManager.AddComponent<SendRpcCommandRequest>(rpcCmd);
    }

    public void ServerLoadLevel(int number)
    {
        ServerWorld.GetExistingSystemManaged<LevelLoader>().LoadLevel(number);
    }

    public void AllClientsLoadLevel(int number)
    {
        foreach (var world in World.All)
        {
            if (world.IsClient())
                world.GetExistingSystemManaged<LevelLoader>().LoadLevel(number);
        }
    }

    public void ServerUnloadLevel(int number)
    {
        ServerWorld.GetExistingSystemManaged<LevelLoader>().UnloadLevel(number);
    }

    public void AllClientsUnloadLevel(int number)
    {
        foreach (var world in World.All)
        {
            if (world.IsClient())
                world.GetExistingSystemManaged<LevelLoader>().UnloadLevel(number);
        }
    }

    public void ServerToggleSync()
    {
        ServerWorld.GetExistingSystemManaged<LevelLoader>().ToggleSync();
    }

    public void AllClientsToggleSync()
    {
        World clientOne = null;
        foreach (var world in World.All)
        {
            if (world.IsClient())
            {
                world.GetExistingSystemManaged<LevelLoader>().ToggleSync();
                if (clientOne == null)
                    clientOne = world;
            }
        }

        bool toggleOn = false;
        if (clientOne != null)
        {
            var conQuery = clientOne.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
            var cons = conQuery.ToEntityArray(Allocator.Temp);
            if (cons.Length != 1)
            {
                UnityEngine.Debug.LogError($"First client {clientOne} goes not have any connection established");
                return;
            }
            if (clientOne.EntityManager.HasComponent<NetworkStreamInGame>(cons[0]))
                toggleOn = true;

            // When switching all clients off the server sync on their connections must be disabled as well or bad things happen
            if (!toggleOn)
            {
                conQuery = ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                cons = conQuery.ToEntityArray(Allocator.Temp);
                var conIds = conQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);
                for (int i=0; i < cons.Length; ++i)
                {
                    if (ServerWorld.EntityManager.HasComponent<NetworkStreamInGame>(cons[i]))
                    {
                        UnityEngine.Debug.Log($"[{ServerWorld}] Disable sync on {conIds[i].Value}");
                        ServerWorld.EntityManager.RemoveComponent<NetworkStreamInGame>(cons[i]);
                    }
                }
            }
        }
    }
}

#endif
