using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameObjectSample
{
    public class GameMenu : MonoBehaviour
    {
        private VisualElement root;

        Button m_ExitGameButton;
        Button m_KickFirstButton;
        Button m_KickAllButton;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void OnEnable()
        {
            if (!Netcode.IsServerBuildOnly)
            {
                GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
                if (!Netcode.IsServerRole)
                    Netcode.OnConnectionEvent += OnDisconnect;
            }
        }

        void OnDisconnect(Connection connection, NetcodeConnectionEvent connectionEvent)
        {
            if (connectionEvent.State != ConnectionState.State.Disconnected) return;
            Netcode.OnConnectionEvent -= OnDisconnect;
            SceneManager.LoadScene("0_MainMenu");
            // You need to shutdown yourself if you don't plan to reuse the client world
            Netcode.Shutdown();
        }

        private void OnDisable()
        {
            Netcode.OnConnectionEvent -= OnDisconnect;
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            if (m_ExitGameButton != null)
            {
                m_ExitGameButton.UnregisterCallback<ClickEvent>(OnExitClicked);
                m_KickFirstButton.UnregisterCallback<ClickEvent>(OnKickClicked);
                m_KickAllButton.UnregisterCallback<ClickEvent>(OnKickAllClicked);
            }
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            root = rootElement;
            m_ExitGameButton = (Button)root.Q("exit");
            m_KickFirstButton = (Button)root.Q("kick");
            m_KickAllButton = (Button)root.Q("kickAll");

            m_ExitGameButton.RegisterCallback<ClickEvent>(OnExitClicked);
            m_KickFirstButton.RegisterCallback<ClickEvent>(OnKickClicked);
            m_KickAllButton.RegisterCallback<ClickEvent>(OnKickAllClicked);
        }

        void OnKickAllClicked(ClickEvent evt)
        {
            Netcode.RequestDisconnectAllClients();
        }

        void OnKickClicked(ClickEvent evt)
        {
            Netcode.AllConnections[0].RequestDisconnect();
        }

        void OnExitClicked(ClickEvent evt)
        {
            Netcode.Shutdown();
            SceneManager.LoadScene("0_MainMenu");
        }
    }
}
