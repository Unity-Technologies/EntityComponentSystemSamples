#if (!UNITY_SERVER || UNITY_EDITOR)
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

    public void SendLoadNextLevelCommand()
    {
        if (ClientServerBootstrap.ClientWorld == null)
        {
            UnityEngine.Debug.LogError("No client world found");
            return;
        }

        var rpcCmd = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntity();
        ClientServerBootstrap.ClientWorld.EntityManager.AddComponentData(rpcCmd, new LoadNextLevelCommand());
        ClientServerBootstrap.ClientWorld.EntityManager.AddComponent<SendRpcCommandRequest>(rpcCmd);
    }

    public void ServerLoadLevel(int number)
    {
        if (ClientServerBootstrap.ServerWorld != null)
            ClientServerBootstrap.ServerWorld.GetExistingSystemManaged<LevelLoader>().LoadLevel(number);
    }

    public void AllClientsLoadLevel(int number)
    {
        if (ClientServerBootstrap.ClientWorld != null)
            ClientServerBootstrap.ClientWorld.GetExistingSystemManaged<LevelLoader>().LoadLevel(number);
    }

    public void ServerUnloadLevel(int number)
    {
        if (ClientServerBootstrap.ServerWorld != null)
            ClientServerBootstrap.ServerWorld.GetExistingSystemManaged<LevelLoader>().UnloadLevel(number);
    }

    public void AllClientsUnloadLevel(int number)
    {
        if (ClientServerBootstrap.ClientWorld != null)
            ClientServerBootstrap.ClientWorld.GetExistingSystemManaged<LevelLoader>().UnloadLevel(number);
    }

    public void ServerToggleSync()
    {
        if (ClientServerBootstrap.ServerWorld != null)
            ClientServerBootstrap.ServerWorld.GetExistingSystemManaged<LevelLoader>().ToggleSync();
    }

    public void AllClientsToggleSync()
    {
        if (ClientServerBootstrap.ClientWorld == null)
            return;

        ClientServerBootstrap.ClientWorld.GetExistingSystemManaged<LevelLoader>().ToggleSync();

        bool toggleOn = false;
        var conQuery = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
        var cons = conQuery.ToEntityArray(Allocator.Temp);
        if (cons.Length != 1)
        {
            UnityEngine.Debug.LogError($"First client {ClientServerBootstrap.ClientWorld} goes not have any connection established");
            return;
        }
        if (ClientServerBootstrap.ClientWorld.EntityManager.HasComponent<NetworkStreamInGame>(cons[0]))
            toggleOn = true;

        // When switching all clients off the server sync on their connections must be disabled as well or bad things happen
        if (!toggleOn)
        {
            conQuery = ClientServerBootstrap.ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
            cons = conQuery.ToEntityArray(Allocator.Temp);
            var conIds = conQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);
            for (int i=0; i < cons.Length; ++i)
            {
                if (ClientServerBootstrap.ServerWorld.EntityManager.HasComponent<NetworkStreamInGame>(cons[i]))
                {
                    UnityEngine.Debug.Log($"[{ClientServerBootstrap.ServerWorld}] Disable sync on {conIds[i].Value}");
                    ClientServerBootstrap.ServerWorld.EntityManager.RemoveComponent<NetworkStreamInGame>(cons[i]);
                }
            }
        }
    }
}

#endif
