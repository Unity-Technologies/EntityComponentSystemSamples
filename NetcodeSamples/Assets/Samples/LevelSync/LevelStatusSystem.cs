#if (!UNITY_SERVER || UNITY_EDITOR)
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UI;

[RequireMatchingQueriesForUpdate]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
public partial class LevelStatusSystem : SystemBase
{
    private Text m_StatusText;
    private Button m_SyncButton;
    private string m_Prefix;
    private bool m_IsServer;

    protected override void OnCreate()
    {
        var levelManager = Object.FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
        {
            Enabled = false;
            return;
        }
        if (World.IsServer())
        {
            m_StatusText = levelManager.ServerStatus;
            m_SyncButton = levelManager.ServerSyncButton;
            m_IsServer = true;
        }
        else if (World.IsClient())
        {
            m_StatusText = levelManager.ClientStatus;
            m_SyncButton = levelManager.ClientSyncButton;
        }
        else
        {
            Enabled = false;
        }
        m_Prefix = World.Name + " ";
    }

    protected override void OnUpdate()
    {
        var sceneSystem = World.GetExistingSystem<SceneSystem>();
        m_StatusText.text = m_Prefix;
        if (m_IsServer)
        {
            FixedString512Bytes inGame = "";
            bool syncOn = false;
            Entities.ForEach((Entity connEntity, in NetworkId netId) =>
            {
                if (SystemAPI.HasComponent<NetworkStreamInGame>(connEntity))
                {
                    var appendMe = FixedString.Format("[{0}]:ON", netId.Value);
                    inGame.Append(appendMe);
                    syncOn = true;
                }
                else
                {
                    var appendMe = FixedString.Format("[{0}]:OFF", netId.Value);
                    inGame.Append(appendMe);
                    syncOn = false;
                }
            }).Run();
            m_StatusText.text += inGame;
            // Flip toggle to off if it's is on currently but sync is disabled
            if (syncOn)
                m_SyncButton.image.color = Color.gray;
            else
                m_SyncButton.image.color = Color.white;
        }
        else
        {
            if (SystemAPI.HasSingleton<NetworkStreamInGame>())
            {
                m_StatusText.text += "ON";
                m_SyncButton.image.color = Color.gray;
            }
            else
            {
                m_StatusText.text += "OFF";
                m_SyncButton.image.color = Color.white;
            }
        }

        m_StatusText.text += ": ";

        FixedString512Bytes levels = "";
        Entities.WithoutBurst().ForEach((Entity sceneEntity, in SceneReference subscene) =>
        {
            if (SceneSystem.IsSceneLoaded(World.Unmanaged, sceneEntity))
            {
                EntityManager.GetName(sceneEntity, out var debugName);
                if (debugName.IsEmpty)
                    levels.Append(subscene.SceneGUID.ToString());
                else
                    levels.Append(debugName);
                levels += " ";
            }
        }).Run();
        m_StatusText.text += levels;
    }
}
#endif
